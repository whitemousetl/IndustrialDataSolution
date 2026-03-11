using IndustrialDataProcessor.Application.Services;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Application.Features;

/// <summary>
/// 工作站配置已更新事件
/// </summary>
public class WorkstationConfigUpdatedEvent : INotification
{
    public DateTime UpdatedTime { get; } = DateTime.Now;
}

/// <summary>
/// 工作站配置更新事件处理器
/// 采用 Fire-and-Forget 模式，后台异步执行耗时操作，不阻塞 API 响应
/// </summary>
/// <remarks>
/// 注意：一个事件可以有多个 Handler（发布-订阅模式）
/// 如果需要添加新的事件处理逻辑，可以创建新的 Handler 类实现 INotificationHandler
/// </remarks>
public class WorkstationConfigUpdatedEventHandler(
    IConnectionManager connectionManager, 
    ICollectionTaskManager taskManager, 
    ILogger<WorkstationConfigUpdatedEventHandler> logger,
    IDataPublishServerManager dataPublishServerManager) : INotificationHandler<WorkstationConfigUpdatedEvent>
{
    private readonly IConnectionManager _connectionManager = connectionManager;
    private readonly ICollectionTaskManager _taskManager = taskManager;
    private readonly ILogger<WorkstationConfigUpdatedEventHandler> _logger = logger;
    private readonly IDataPublishServerManager _dataPublishServerManager = dataPublishServerManager;

    public Task Handle(WorkstationConfigUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到配置更新事件，后台任务将异步执行。时间: {Time}", notification.UpdatedTime);

        // 【性能优化】使用 Fire-and-Forget 模式，立即返回不阻塞调用方
        // 耗时操作（连接关闭、任务重启、OPC UA 服务器重启）在后台异步执行
        _ = ExecuteBackgroundOperationsAsync(cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 后台异步执行耗时操作
    /// </summary>
    private async Task ExecuteBackgroundOperationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始执行后台配置更新操作...");

            // 1. 关闭底层的旧物理连接（避免地址占用的冲突）
            await _connectionManager.ClearAllConnectionsAsync();

            // 2. 命令任务管理器，杀掉所有旧线程，重新去数据库读最新配置并启动新线程
            await _taskManager.StartOrRestartAllTasksAsync(cancellationToken);

            // 3. 杀掉并基于最新配置重新生成数据发布服务器 
            await _dataPublishServerManager.StartOrRestartServerAsync();

            _logger.LogInformation("后台配置更新操作完成，所有服务已切换到最新配置。");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("后台配置更新操作被取消。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "后台配置更新操作执行失败。");
        }
    }
}
