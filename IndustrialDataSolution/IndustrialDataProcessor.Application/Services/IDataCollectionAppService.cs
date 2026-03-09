namespace IndustrialDataProcessor.Application.Services;

/// <summary>
/// 数据采集应用服务接口 (由后台托管服务调用)
/// </summary>
public interface IDataCollectionAppService
{
    /// <summary>
    /// 初始化并启动所有协议的独立采集任务（后台常驻）
    /// </summary>
    Task StartAllProtocolCollectionTasksAsync(CancellationToken globalToken);
}
