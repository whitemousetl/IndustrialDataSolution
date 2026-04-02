using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.HeartbeatReporter;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.BackgroundServices;

/// <summary>
/// 设备状态心跳上报后台服务
/// <para>与数据采集流程完全独立，无论采集成功或失败均按配置间隔持续上报。
/// 具备异常恢复能力，网络中断后自动重试，服务生命周期与应用程序保持一致。</para>
/// </summary>
public class HeartbeatReporterHostingService(
    DataCollectionChannel channel,
    IWorkstationConfigCache configCache,
    IHttpClientFactory httpClientFactory,
    IOptions<HeartbeatOptions> options,
    ILogger<HeartbeatReporterHostingService> logger) : BackgroundService
{
    private readonly DataCollectionChannel _channel = channel;
    private readonly IWorkstationConfigCache _configCache = configCache;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly HeartbeatOptions _options = options.Value;
    private readonly ILogger<HeartbeatReporterHostingService> _logger = logger;

    // 记录每个设备的最新采集状态（key = equipmentId）
    // 使用 ConcurrentDictionary 保证线程安全，通道消费者和定时器任务并发读写
    private readonly ConcurrentDictionary<string, LatestEquipmentEntry> _latestResults = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("[心跳上报] 功能已禁用（HeartbeatReporter:Enabled=false），服务不启动。");
            return;
        }

        _logger.LogInformation("[心跳上报] 服务启动，上报端点: {Endpoint}，间隔: {Interval}s",
            _options.Endpoint, _options.IntervalSeconds);

        // 从配置预初始化设备状态（确保程序启动时即使没有采集数据也能上报）
        await InitializeFromConfigAsync();

        // 两个任务完全独立运行：
        //   1. 通道消费者 —— 持续更新最新采集状态（不影响定时器）
        //   2. 定时上报器 —— 按间隔定时发送心跳（不依赖通道是否有数据）
        // 使用 _ = Task.Run 让两者独立运行，任何一个崩溃不影响另一个，
        // 并且不会导致整个 ExecuteAsync 退出。
        _ = RunChannelConsumerWithRestartAsync(stoppingToken);

        // 定时上报是主循环，直接 await，与服务生命周期绑定
        await RunTimerLoopAsync(stoppingToken);
    }

    /// <summary>
    /// 从配置缓存初始化设备列表，赋予"待采集"的初始状态。
    /// 确保程序刚启动还没有采集数据时，心跳中也能包含设备条目。
    /// </summary>
    private async Task InitializeFromConfigAsync()
    {
        // 等待配置缓存准备好（最多等 30 秒）
        var deadline = DateTime.UtcNow.AddSeconds(30);
        WorkstationConfig? workstation = null;
        while (DateTime.UtcNow < deadline)
        {
            workstation = _configCache.GetCurrentConfig();
            if (workstation != null) break;
            await Task.Delay(500);
        }

        if (workstation == null)
        {
            _logger.LogWarning("[心跳上报] 配置缓存在 30 秒内未就绪，将以空设备列表启动。");
            return;
        }

        foreach (var protocol in workstation.Protocols)
        {
            string protocolIp = protocol is NetworkProtocolConfig netConfig
                ? netConfig.IpAddress
                : string.Empty;

            foreach (var equipment in protocol.Equipments)
            {
                _latestResults.TryAdd(equipment.Id, new LatestEquipmentEntry(
                    EquipmentId: equipment.Id,
                    EquipmentName: equipment.Name ?? equipment.Id,
                    EquipmentIP: protocolIp,
                    ReadIsSuccess: false,
                    StartTime: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Msg: "等待采集",
                    Info: "服务已启动，等待第一次采集结果"
                ));
            }
        }

        _logger.LogInformation("[心跳上报] 已从配置初始化 {Count} 台设备的初始状态。", _latestResults.Count);
    }

    /// <summary>
    /// 带自动重启的通道消费者守护循环。
    /// 即使通道消费过程中发生意外异常，也会自动重启，不影响定时上报。
    /// </summary>
    private async Task RunChannelConsumerWithRestartAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeChannelAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 服务正常停止，退出循环
                break;
            }
            catch (Exception ex)
            {
                // 意外异常：记录日志后等待 5 秒重启，不影响定时上报
                _logger.LogError(ex, "[心跳上报] 通道消费者异常，5 秒后自动重启。");
                try { await Task.Delay(5000, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    /// <summary>
    /// 定时上报主循环。
    /// 与通道消费者完全独立：即使采集数据为空、网络中断，此循环也会持续运行。
    /// </summary>
    private async Task RunTimerLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SendHeartbeatAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw; // 服务停止，向上传播
                }
                catch (Exception ex)
                {
                    // 单次上报失败，记录日志后继续等待下一个 tick，不中断定时器
                    _logger.LogError(ex, "[心跳上报] 本次上报发生未预期异常，下一周期将重试。");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 服务正常停止
        }
    }

    /// <summary>
    /// 持续消费心跳通道，将最新的设备状态存入字典
    /// </summary>
    private async Task ConsumeChannelAsync(CancellationToken stoppingToken)
    {
        await foreach (var (result, config, _) in _channel.HeartbeatChannel.ReadAllAsync(stoppingToken))
        {
            string protocolIp = config is NetworkProtocolConfig netConfig
                ? netConfig.IpAddress
                : string.Empty;

            foreach (var eqResult in result.EquipmentResults)
            {
                string equipName = eqResult.EquipmentName;
                if (string.IsNullOrEmpty(equipName))
                {
                    var equipConfig = config.Equipments.FirstOrDefault(e => e.Id == eqResult.EquipmentId);
                    equipName = equipConfig?.Name ?? eqResult.EquipmentId;
                }

                _latestResults[eqResult.EquipmentId] = new LatestEquipmentEntry(
                    EquipmentId: eqResult.EquipmentId,
                    EquipmentName: equipName,
                    EquipmentIP: protocolIp,
                    ReadIsSuccess: eqResult.ReadIsSuccess,
                    StartTime: eqResult.StartTime,
                    Msg: eqResult.ReadIsSuccess ? "采集成功" : (eqResult.ErrorMsg ?? "采集失败"),
                    Info: BuildInfoMessage(eqResult)
                );
            }
        }
    }

    /// <summary>
    /// 构造并发送一次心跳请求。
    /// 无论 _latestResults 是否为空均会发送（空时 EquipmentResults 为空数组）。
    /// 网络异常时记录日志并抛出，由调用方决定是否重试。
    /// </summary>
    private async Task SendHeartbeatAsync(CancellationToken stoppingToken)
    {
        var workstation = _configCache.GetCurrentConfig();
        string workstationId = workstation?.Id ?? string.Empty;
        string workstationIp = workstation?.IpAddress ?? string.Empty;

        var equipmentResults = _latestResults.Values.Select(e => new
        {
            e.EquipmentId,
            e.EquipmentName,
            e.EquipmentIP,
            e.ReadIsSuccess,
            e.StartTime,
            msg = e.Msg,
            info = e.Info
        }).ToList();

        var payload = new
        {
            WorkstationId = workstationId,
            IP = workstationIp,
            EquipmentResults = equipmentResults
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var client = _httpClientFactory.CreateClient("HeartbeatReporter");
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("[心跳上报] 正在上报 {Count} 台设备状态到 {Endpoint}",
            equipmentResults.Count, _options.Endpoint);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(_options.Endpoint, content, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 网络层异常（连接超时、DNS 解析失败等），记录后由定时器循环决定重试
            _logger.LogWarning(ex, "[心跳上报] 网络异常，端点: {Endpoint}，下一周期自动重试。", _options.Endpoint);
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[心跳上报] 上报成功，状态码: {StatusCode}，设备数: {Count}",
                (int)response.StatusCode, equipmentResults.Count);
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync(stoppingToken);
            _logger.LogWarning("[心跳上报] 服务端返回非成功状态码: {StatusCode}，响应: {Body}",
                (int)response.StatusCode, body);
        }
    }

    /// <summary>
    /// 根据设备结果构造详细信息字符串
    /// </summary>
    private static string BuildInfoMessage(EquipmentResult eqResult)
    {
        if (!eqResult.ReadIsSuccess)
            return eqResult.ErrorMsg ?? "设备采集失败";

        if (eqResult.FailedPoints > 0)
            return $"{eqResult.SuccessPoints}/{eqResult.TotalPoints} 个测点正常，{eqResult.FailedPoints} 个测点异常";

        return "所有测点正常";
    }

    // 内部记录类，存储最新设备状态快照
    private sealed record LatestEquipmentEntry(
        string EquipmentId,
        string EquipmentName,
        string EquipmentIP,
        bool ReadIsSuccess,
        string StartTime,
        string Msg,
        string Info);
}
