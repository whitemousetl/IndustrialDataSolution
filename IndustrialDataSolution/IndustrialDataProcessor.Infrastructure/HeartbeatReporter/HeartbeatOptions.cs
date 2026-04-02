namespace IndustrialDataProcessor.Infrastructure.HeartbeatReporter;

/// <summary>
/// 设备状态心跳上报配置选项
/// </summary>
public class HeartbeatOptions
{
    public const string SectionName = "HeartbeatReporter";

    /// <summary>
    /// 是否启用心跳上报（默认启用）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 上报接口地址
    /// </summary>
    public string Endpoint { get; set; } = "http://10.9.44.20:8000/iot/api/v1/Equipment/heartbeat";

    /// <summary>
    /// 上报间隔（秒），默认 60 秒
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// HTTP 请求超时时间（秒），默认 10 秒
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
