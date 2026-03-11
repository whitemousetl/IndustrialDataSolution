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
    PointExpressionConverter pointExpressionConverter) : BackgroundService, IDataPublishServerManager
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
            _logger.LogInformation("准备启动/重启 OPC UA 服务器...");

            // 1. 如果之前有服务器在跑，发信号取消并停止它
            if (_currentCts != null && !_currentCts.IsCancellationRequested)
            {
                _logger.LogInformation("正在停止旧的 OPC UA 服务器实例...");
                await _currentCts.CancelAsync();

                if (_opcServer != null)
                {
                    await _opcServer.StopAsync(); // 停止底层 OPC Server
                    _opcServer = null;
                }

                // 可选的一小段延时，确保端口已经被完全释放
                await Task.Delay(1000, _appStoppingToken);
                _currentCts.Dispose();
            }

            // 2. 创建针对新一轮运行生命周期的 CancellationToken
            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(_appStoppingToken);

            // 3. 将服务器核心逻辑丢到后台执行（类似于任务管理器）
            _ = RunServerLoopAsync(_currentCts.Token);

            _logger.LogInformation("OPC UA 服务器重启任务已在后台下发完毕。");
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
            // 1. 获取配置以启动服务器 (通过创建一个新的 Scope 解析 Repository)
            WorkstationConfig? workstationConfig = null;

            using (var scope = serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IWorkstationConfigRepository>();
                using (_logger.BeginScope("Caller: {CallerService}", nameof(OpcUaHostingService)))
                    workstationConfig = await repository.GetLatestParsedConfigAsync(loopToken);
            }

            if (workstationConfig == null) return;

            // 2. 启动 OPC UA Server
            var app = new ApplicationInstance((ITelemetryContext)null!)
            {
                ApplicationName = "WorkstationOpcServer",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "WorkstationOpcServer"
            };

            var opcUaServerConfig = await CreateServerConfigurationAsync();
            app.ApplicationConfiguration = opcUaServerConfig;
            await app.CheckApplicationInstanceCertificatesAsync(false, 0, loopToken);

            _opcServer = new WorkstationOpcServer(workstationConfig);

            // 注意: 使用 app.StartAsync 启动服务
            await app.StartAsync(_opcServer);

            _logger.LogInformation("最新的 OPC UA 服务器已挂载并开始运行监听...");

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
            _logger.LogInformation("OPC UA 服务器当前运作循环已被取消/终止。");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "OPC UA 服务器在运行过程中发生致命错误！");
        }
    }

    private static async Task<ApplicationConfiguration> CreateServerConfigurationAsync()
    {
        var config = new ApplicationConfiguration()
        {
            ApplicationName = "WorkstationOpcServer",
            ApplicationUri = $"urn:{Utils.GetHostName()}:WorkstationOpcServer",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier { StoreType = "Directory", StorePath = "pki/own", SubjectName = "CN=WorkstationOpcServer" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/trusted" },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/issuers" },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/rejected" },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = ["opc.tcp://0.0.0.0:4840/WorkstationServer"],
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