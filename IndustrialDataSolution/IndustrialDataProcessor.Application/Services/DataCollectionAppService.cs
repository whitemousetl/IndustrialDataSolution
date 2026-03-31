using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IndustrialDataProcessor.Application.Services;

public class DataCollectionAppService(
    IWorkstationConfigRepository repository,
    IWorkstationConfigCache configCache,
    IConnectionManager connectionManager,
    IEnumerable<IProtocolDriver> drivers,
    ILogger<DataCollectionAppService> logger,
    DataCollectionChannel dataChannel,
    IEquipmentDataProcessor equipmentDataProcessor) : IDataCollectionAppService
{
    private readonly IWorkstationConfigRepository _repository = repository;
    private readonly IWorkstationConfigCache _configCache = configCache;
    private readonly IConnectionManager _connectionManager = connectionManager;
    private readonly IEnumerable<IProtocolDriver> _drivers = drivers;
    private readonly ILogger<DataCollectionAppService> _logger = logger;
    private readonly DataCollectionChannel _dataChannel = dataChannel;
    private readonly IEquipmentDataProcessor _equipmentDataProcessor = equipmentDataProcessor;

    // 配置监控轮询间隔（毫秒）
    private const int ConfigMonitorIntervalMs = 5000;

    /// <summary>
    /// 初始化并启动所有协议的独立采集任务（后台常驻）
    /// <para>当配置为空时，会自动启动配置监控模式，定期检查新配置</para>
    /// </summary>
    public async Task StartAllProtocolCollectionTasksAsync(CancellationToken token)
    {
        // 1. 使用缓存获取配置，避免重复数据库查询
        var workstation = await _configCache.GetOrLoadAsync(_repository, token);

        // 2. 检查配置有效性
        if (workstation == null || workstation.Protocols == null || workstation.Protocols.Count == 0)
        {
            _logger.LogWarning("[数据采集] 当前无可用配置数据，启动配置监控模式（每 {IntervalMs}ms 检查一次）...",
                ConfigMonitorIntervalMs);

            // 启动配置监控任务，定期检查配置是否可用
            _ = Task.Run(() => MonitorConfigAndStartCollectionAsync(token), token);
            return;
        }

        // 3. 配置存在，直接启动采集任务
        await StartCollectionWithConfigAsync(workstation, token);
    }

    /// <summary>
    /// 配置监控循环：定期检查配置是否可用，一旦获取到配置立即启动采集
    /// </summary>
    private async Task MonitorConfigAndStartCollectionAsync(CancellationToken token)
    {
        _logger.LogInformation("[数据采集] 配置监控任务已启动，等待配置下发...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                // 清空缓存，强制重新从数据库加载
                _configCache.ClearCache();

                // 尝试获取配置
                var workstation = await _configCache.GetOrLoadAsync(_repository, token);

                if (workstation != null && workstation.Protocols != null && workstation.Protocols.Count > 0)
                {
                    _logger.LogInformation("[数据采集] 监控到新的配置数据，立即启动采集服务...");

                    // 启动采集任务
                    await StartCollectionWithConfigAsync(workstation, token);

                    _logger.LogInformation("[数据采集] 配置监控任务已完成，采集服务已启动");
                    return;
                }

                // 配置仍不可用，等待后重试
                _logger.LogDebug("[数据采集] 配置监控：当前无可用配置，{IntervalMs}ms 后重试...",
                    ConfigMonitorIntervalMs);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出监控
                _logger.LogInformation("[数据采集] 配置监控任务已取消");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[数据采集] 配置监控过程中发生异常，{IntervalMs}ms 后重试...",
                    ConfigMonitorIntervalMs);
            }

            // 等待下一次检查
            try
            {
                await Task.Delay(ConfigMonitorIntervalMs, token);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("[数据采集] 配置监控任务在等待期间被取消");
                return;
            }
        }
    }

    /// <summary>
    /// 使用指定配置启动采集任务
    /// </summary>
    private async Task StartCollectionWithConfigAsync(WorkstationConfig workstation, CancellationToken token)
    {
        _logger.LogInformation("[数据采集] 成功加载配置，包含 {ProtocolCount} 个协议，缓存版本: {CacheVersion}",
            workstation.Protocols.Count, _configCache.Version);

        // 针对每个协议，创建一个游离不阻塞的 Task.Run (在后台独立循环)
        foreach (var protocol in workstation.Protocols)
        {
            _logger.LogInformation("[数据采集] 启动协议 [{ProtocolId}] 的采集线程，协议类型: {ProtocolType}",
                protocol.Id, protocol.ProtocolType);

            // 通过 Task.Run 让这个长循环在线程池上跑，互不影响
            _ = Task.Run(() => LongRunningProtocolCycleAsync(protocol, token), token);
        }

        _logger.LogInformation("[数据采集] 所有协议采集线程已启动，共 {Count} 个", workstation.Protocols.Count);
    }

    /// <summary>
    /// 针对某一个单一协议，后台执行的独立长驻循环
    /// </summary>
    private async Task LongRunningProtocolCycleAsync(ProtocolConfig protocol, CancellationToken token)
    {
        _logger.LogInformation(">>> 协议 [{ProtocolId}] 的独立采集线程已启动.", protocol.Id);

        // 获取对应的驱动 (一次性获取即可)
        var driver = _drivers.FirstOrDefault(d => d.GetProtocolName() == protocol.ProtocolType.ToString());
        if (driver == null)
        {
            _logger.LogError("协议 [{ProtocolId}] 找不到支持的驱动 {Type}，该协议线程终止。", protocol.Id, protocol.ProtocolType);
            return;
        }

        // 只要宿主程序未退出，该协议就一直根据其延时设定不断循环轮询
        while (!token.IsCancellationRequested)
        {
            var swProtocol = Stopwatch.StartNew();
            var startTimeProtocol = DateTime.Now;
            var activeEquipments = protocol.Equipments.Where(e => e.IsCollect).ToList();

            var protocolResult = new ProtocolResult
            {
                Id = Guid.NewGuid().ToString("N"), // 当前批次的唯一标识
                ProtocolId = protocol.Id,
                ProtocolType = protocol.ProtocolType.ToString(),
                InterfaceType = protocol.InterfaceType,
                StartTime = startTimeProtocol.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ReadIsSuccess = true,
                TotalEquipments = activeEquipments.Count,             
                EquipmentResults = []
            };

            try
            {
                // 1. 获取当前长生命周期的连接 (带复用机制)。如果这里抛异常，说明根本连不上，进入全局Catch
                var handle = await _connectionManager.GetOrCreateConnectionAsync(protocol, token);

                // 2. 依次读取当前协议下的启用的设备
                foreach (var equipment in activeEquipments)
                {
                    if (equipment.Parameters == null) continue;

                    var swReq = Stopwatch.StartNew();
                    var equipmentResult = new EquipmentResult
                    {
                        EquipmentId = equipment.Id,
                        EquipmentName = equipment.Name ?? string.Empty,
                        StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        TotalPoints = equipment.Parameters.Count,
                        PointResults = []
                    };

                    foreach (var point in equipment.Parameters)
                    {
                        // 取消判断
                        if (token.IsCancellationRequested) break;

                        var swPoint = Stopwatch.StartNew();
                        PointResult? pointResult = null;

                        // 【核心改动 1】拦截虚拟点，直接构造默认成功的占位结果，不交给底层物理驱动
                        if (!string.IsNullOrEmpty(point.Address) && point.Address.Contains("VirtualPoint", StringComparison.OrdinalIgnoreCase))
                        {
                            pointResult = new PointResult
                            {
                                Label = point.Label,
                                Address = point.Address,
                                DataType = point.DataType,
                                ReadIsSuccess = true,  // 默认采集成功，等管道后方的 Processor 基于公式计算结果重新仲裁
                                ErrorMsg = string.Empty,
                                ElapsedMs = 0,
                                Value = null
                            };
                        }
                        else
                        {
                            try
                            {
                                pointResult = await driver.ReadAsync(handle, protocol, equipment.Id, point, token);
                                if (pointResult != null)
                                {
                                    if(!pointResult.ReadIsSuccess) 
                                        _logger.LogError($"[数据采集] 设备 {equipment.Id} Label {point.Label} 读取失败，信息{pointResult.ErrorMsg}");
                                    pointResult.ElapsedMs = swPoint.ElapsedMilliseconds;
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                pointResult = new PointResult
                                {
                                    Label = point.Label,
                                    Address = point.Address,
                                    DataType = point.DataType,
                                    ReadIsSuccess = false,
                                    ErrorMsg = ex.InnerException?.Message ?? ex.Message,
                                    ElapsedMs = swPoint.ElapsedMilliseconds
                                };
                            }
                        }

                        if (pointResult != null)
                        {
                            equipmentResult.PointResults.Add(pointResult);
                        }
                    }

                    // 填写设备的物理耗时（不再在这里计算任何 SuccessPoints/FailedPoints 等聚合数据）
                    swReq.Stop();
                    equipmentResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    equipmentResult.ElapsedMs = swReq.ElapsedMilliseconds;

                    protocolResult.EquipmentResults.Add(equipmentResult);
                }
            }
            catch (OperationCanceledException)
            {
                // 收到退出信号，正常结束
                break;
            }
            catch (Exception ex)
            {
                // 单个协议内的异常不会导致本协议线程崩溃，更不会影响其它协议。
                // 捕获后记录日志，然后等待下一次循环重试。
                // 连接获取失败 或 全局性的毁灭异常
                // 能够来到这里的，说明是极其严重的底层连接异常。
                // 这个异常状态需要在采集层就定死，通知下游不需再算了。
                protocolResult.ReadIsSuccess = false;
                protocolResult.ErrorMsg = $"协议级异常: {ex.Message}";
                protocolResult.FailedEquipments = protocolResult.TotalEquipments;

                _logger.LogError(ex, "[数据采集] 协议 [{ProtocolId}] 采集周期发生异常，将在 {DelayMs}ms 后重试",
                    protocol.Id, protocol.CommunicationDelay);

                // 连接级异常发生时，主动将已缓存的连接删除并关闭
                // 避免下次重试时复用已断开的僵尸连接，导致 PLC 设备的连接槽泭將1被占满
                await _connectionManager.InvalidateConnectionAsync(protocol.Id);
            }
            finally
            {
                // 完成 Protocol层 的耗时和时间收尾计算
                swProtocol.Stop();
                protocolResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                protocolResult.Time = protocolResult.EndTime;
                protocolResult.ElapsedMs = swProtocol.ElapsedMilliseconds;

                if (protocolResult.ReadIsSuccess)
                    // 协议级成败：没有任何失败的设备或异常就是成功
                    protocolResult.ReadIsSuccess = protocolResult.FailedEquipments == 0;

                // 【无论成功还是产生异常跌落，都将携带状态的结果推送出去，避免系统收不到断线状态】
                try
                {
                    // 【核心改动：在数据入管道扇分之前，先进行加工算好绝对值和聚合状态】
                    // 并且我们拿到它的 Map，把它一起送到通道里！
                    var dictMap = _equipmentDataProcessor.Process(protocolResult, protocol, token);

                    // 一键扇分给所有消费者
                    await _dataChannel.PublishAsync(protocolResult, protocol, dictMap, token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "[数据采集] 写入数据通道失败，协议: {ProtocolId}", protocol.Id);
                }
            }

            // ============================================
            // 核心：这里的 Delay 是这根专属线程自己的延迟，
            // 也就是说这个协议执行得快慢、延时多少，完全不影响别的协议的线程进度的！
            // ============================================
            try
            {
                // 判断延时配置是否有效，最少避让 1 毫秒防 CPU 飙高
                int delayMs = protocol.CommunicationDelay > 0 ? protocol.CommunicationDelay : 1;
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException) { break; }
        }

        _logger.LogInformation("<<< 协议 [{ProtocolId}] 的独立采集线程已退出.", protocol.Id);
    }
}
