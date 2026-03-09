using IndustrialDataProcessor.Application.Services;

namespace IndustrialDataProcessor.Api.BackgroundServices;

/// <summary>
/// 数据采集后台托管服务，负责定时触发应用层的采集逻辑
/// </summary>
public class DataCollectionHostedService(
    ILogger<DataCollectionHostedService> logger, 
    ICollectionTaskManager taskManager) : BackgroundService
{
    private readonly ICollectionTaskManager _taskManager = taskManager;
    private readonly ILogger<DataCollectionHostedService> _logger = logger;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("工业数据采集后台服务已启动...");

        // 启动任务管理器 (注意传入 stoppingToken 保证程序关闭时能停掉子线程)
        await _taskManager.StartOrRestartAllTasksAsync(stoppingToken);

        // 让主线程挂起，直到程序结束
        await Task.Delay(Timeout.Infinite, stoppingToken);

        _logger.LogInformation("工业数据采集后台服务已停止。");
    }
}
