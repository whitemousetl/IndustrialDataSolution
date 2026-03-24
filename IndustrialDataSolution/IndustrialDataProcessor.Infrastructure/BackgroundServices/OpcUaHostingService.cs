using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;
using IndustrialDataProcessor.Infrastructure.OpcUa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Configuration;

namespace IndustrialDataProcessor.Infrastructure.BackgroundServices;

/// <summary>
/// 这是运行在基础设施层的后台服务
/// 负责：1. 启动 OPC UA 服务器。 2. 监听通道里的数据并更新给 OPC
/// </summary>
public class OpcUaHostingService(
    IServiceProvider serviceProvider, // 解决 DI 生命周期的关键：注入 IServiceProvider
    DataCollectionChannel dataChannel,
    ILogger<OpcUaHostingService> logger,
    IConnectionManager connectionManager,
    IEnumerable<IProtocolDriver> drivers,
    PointExpressionConverter pointExpressionConverter,
    IWorkstationConfigCache configCache,
    IOptions<OpcUaOptions> opcUaOptions) : BackgroundService, IDataPublishServerManager
{
    private WorkstationOpcServer? _opcServer;

    // 用于控制当前这一批正在运行的 OPC UA 服务器任务的 TokenSource
    private CancellationTokenSource? _currentCts;
    // 保护重启操作不能并发执行
    private readonly SemaphoreSlim _restartLock = new(1, 1);
    private readonly ILogger<OpcUaHostingService> _logger = logger;

    // 连接管理器
    private readonly IConnectionManager _connectionManager = connectionManager; // 连接管理器
    private readonly IEnumerable<IProtocolDriver> _drivers = drivers;
    private readonly PointExpressionConverter _pointExpressionConverter = pointExpressionConverter;
    private readonly IWorkstationConfigCache _configCache = configCache;
    private readonly OpcUaOptions _opcUaOptions = opcUaOptions.Value;

    // 宿主程序退出时的最终 Token
    private CancellationToken _appStoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _appStoppingToken = stoppingToken;

        // 首次启动服务器
        await StartOrRestartServerAsync();

        try
        {
            // 挂起 BackgroundService，保活直到宿主下达全局 Cancellation 信号
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // 忽略由于宿主正常停止而抛出的取消异常
        }
    }

    public async Task StartOrRestartServerAsync()
    {
        await _restartLock.WaitAsync(_appStoppingToken);
        try
        {
            _logger.LogInformation("[OPC UA] 开始启动/重启服务器...");

            // 1. 如果之前有服务器在跑，发信号取消并停止它
            if (_currentCts != null && !_currentCts.IsCancellationRequested)
            {
                _logger.LogInformation("[OPC UA] 正在停止旧的服务器实例...");
                await _currentCts.CancelAsync();

                if (_opcServer != null)
                {
                    await _opcServer.StopAsync();
                    _opcServer = null;
                }

                // 延时确保端口已经完全释放
                await Task.Delay(1000, _appStoppingToken);
                _currentCts.Dispose();
                _logger.LogInformation("[OPC UA] 旧服务器实例已停止");
            }

            // 2. 创建针对新一轮运行生命周期的 CancellationToken
            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(_appStoppingToken);

            // 3. 将服务器核心逻辑丢到后台执行
            _ = RunServerLoopAsync(_currentCts.Token);

            _logger.LogInformation("[OPC UA] 服务器重启任务已在后台下发完毕");
        }
        finally
        {
            _restartLock.Release();
        }
    }

    private async Task RunServerLoopAsync(CancellationToken loopToken)
    {
        try
        {
            // 1. 使用缓存获取配置，避免重复数据库查询
            WorkstationConfig? workstationConfig = null;

            using (var scope = serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IWorkstationConfigRepository>();
                workstationConfig = await _configCache.GetOrLoadAsync(repository, loopToken);
            }

            // 2. 检查配置有效性
            if (workstationConfig == null)
            {
                _logger.LogWarning("[OPC UA] 无法获取工作站配置，当前无可用配置数据。等待配置更新后自动重启...");
                return;
            }

            _logger.LogInformation("[OPC UA] 成功加载工作站配置，缓存版本: {CacheVersion}, 工作站ID: {WorkstationId}",
                _configCache.Version, workstationConfig.Id);

            // 2. 启动 OPC UA Server
            var app = new ApplicationInstance((ITelemetryContext)null!)
            {
                ApplicationName = "WorkstationOpcServer",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "WorkstationOpcServer"
            };

            var opcUaServerConfig = await CreateServerConfigurationAsync(_opcUaOptions.Endpoint);
            app.ApplicationConfiguration = opcUaServerConfig;
            try
            {
                await app.CheckApplicationInstanceCertificatesAsync(false, 2048, loopToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OPC UA] 证书检查/生成失败，请检查 pki/own 目录权限");
                throw;
            }

            _opcServer = new WorkstationOpcServer(workstationConfig);

            // 启动 OPC UA 服务
            await app.StartAsync(_opcServer);

            _logger.LogInformation("[OPC UA] 服务器已启动并开始监听，服务端点: {Endpoint}", _opcUaOptions.Endpoint);

            // 3. 挂载 OPC 客户端写入事件
            _opcServer.CustomNodeManager.OnOpcClientWriteRequestedAsync += async (protocol, eq, point, value) =>
            {
                var driver = _drivers.FirstOrDefault(d => d.GetProtocolName() == protocol.ProtocolType.ToString());
                if (driver == null)
                    return (false, $"未找到协议类型 {protocol.ProtocolType} 的驱动程序");

                var handler = await _connectionManager.GetOrCreateConnectionAsync(protocol, loopToken);

                // ========================================================
                // 【核心新增】：在这里调用反推逻辑！
                // 将 OPC 客户端发来的业务值，反转计算成机器通讯需要的底层真实物理值
                // ========================================================
                object physicalWriteValue = _pointExpressionConverter.ConvertInverse(point, value) ?? value;

                var writeTask = new WriteTask
                {
                    WritePoint = point
                };

                var result = await driver.WriteAsync(handler, writeTask, physicalWriteValue, loopToken);

                return (result, result ? "Write Success!" : "Write False!");
            };

            // 4. 死循环：从通道拿数据。注意这里必须绑定 loopToken！
            await foreach (var (Result, _, _) in dataChannel.OpcUaChannel.ReadAllAsync(loopToken))
            {
                try
                {
                    if (_opcServer?.CustomNodeManager != null)
                    {
                        _opcServer.CustomNodeManager.UpdateDataFromCollectionResult(Result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新 OPC UA 节点失败");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[OPC UA] 服务器运行循环已正常取消");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[OPC UA] 服务器运行过程中发生致命错误，服务将停止");
        }
    }

    private static async Task<ApplicationConfiguration> CreateServerConfigurationAsync(string endpoint)
    {
        // 以运行目录为基准
        var baseDir = AppContext.BaseDirectory;

        var config = new ApplicationConfiguration()
        {
            ApplicationName = "WorkstationOpcServer",
            ApplicationUri = $"urn:{Utils.GetHostName()}:WorkstationOpcServer",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier { StoreType = "Directory", StorePath = Path.Combine(baseDir, "pki", "own"), SubjectName = "CN=WorkstationOpcServer" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(baseDir, "pki", "trusted") },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(baseDir, "pki", "issuers") },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(baseDir, "pki", "rejected") },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = [endpoint],
                SecurityPolicies = [new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }],
                UserTokenPolicies = [new UserTokenPolicy(UserTokenType.Anonymous)]
            }
        };

        await config.ValidateAsync(ApplicationType.Server);
        return config;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_currentCts != null)
        {
            await _currentCts.CancelAsync();
        }
        if (_opcServer != null)
        {
            await _opcServer.StopAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}