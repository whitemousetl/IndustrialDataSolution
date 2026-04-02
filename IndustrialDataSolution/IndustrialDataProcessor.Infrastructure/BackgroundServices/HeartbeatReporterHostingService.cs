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
/// <para>每隔固定间隔将最新的设备采集状态通过 HTTP POST 上报到远程服务器</para>
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

        // 并发运行：一个任务持续消费Channel更新最新状态，另一个任务定时上报
        var readerTask = ConsumeChannelAsync(stoppingToken);
        var timerTask = SendHeartbeatOnScheduleAsync(stoppingToken);

        await Task.WhenAll(readerTask, timerTask);
    }

    /// <summary>
    /// 持续消费心跳通道，将最新的设备状态存入字典
    /// </summary>
    private async Task ConsumeChannelAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var (result, config, _) in _channel.HeartbeatChannel.ReadAllAsync(stoppingToken))
            {
                // 获取协议级 IP（用作 EquipmentIP）
                string protocolIp = config is NetworkProtocolConfig netConfig
                    ? netConfig.IpAddress
                    : string.Empty;

                foreach (var eqResult in result.EquipmentResults)
                {
                    // 找到对应的设备配置以获取设备名称
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
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    /// <summary>
    /// 定时上报心跳
    /// </summary>
    private async Task SendHeartbeatOnScheduleAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendHeartbeatAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    /// <summary>
    /// 构造并发送一次心跳请求
    /// </summary>
    private async Task SendHeartbeatAsync(CancellationToken stoppingToken)
    {
        if (_latestResults.IsEmpty)
        {
            _logger.LogDebug("[心跳上报] 暂无设备采集结果，跳过本次上报。");
            return;
        }

        // 从配置缓存获取工作站信息
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

        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var client = _httpClientFactory.CreateClient("HeartbeatReporter");
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("[心跳上报] 正在上报 {Count} 台设备状态到 {Endpoint}",
                equipmentResults.Count, _options.Endpoint);

            var response = await client.PostAsync(_options.Endpoint, content, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[心跳上报] 上报成功，状态码: {StatusCode}，设备数: {Count}",
                    (int)response.StatusCode, equipmentResults.Count);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(stoppingToken);
                _logger.LogWarning("[心跳上报] 上报失败，状态码: {StatusCode}，响应: {Body}",
                    (int)response.StatusCode, body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[心跳上报] 上报异常，端点: {Endpoint}", _options.Endpoint);
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
