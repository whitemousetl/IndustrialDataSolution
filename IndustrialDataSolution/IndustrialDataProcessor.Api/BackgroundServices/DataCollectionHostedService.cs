using IndustrialDataProcessor.Application.Services;

namespace IndustrialDataProcessor.Api.BackgroundServices;

/// <summary>
/// 数据采集后台托管服务
/// </summary>
/// <remarks>
/// 此服务属于组合根的一部分，负责启动应用层的采集任务管理器。
/// 它不包含基础设施逻辑，只是编排启动，因此放在 Api 层而非 Infrastructure 层。
/// </remarks>
public class DataCollectionHostedService(
    ILogger<DataCollectionHostedService> logger, 
    ICollectionTaskManager taskManager) : BackgroundService
{
    private readonly ICollectionTaskManager _taskManager = taskManager;
    private readonly ILogger<DataCollectionHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("工业数据采集后台服务已启动...");

        // 启动任务管理器 (注意传入 stoppingToken 保证程序关闭时能停掉子线程)
        await _taskManager.StartOrRestartAllTasksAsync(stoppingToken);

        // 让主线程挂起，直到程序结束
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }

        _logger.LogInformation("工业数据采集后台服务已停止。");
    }
}
