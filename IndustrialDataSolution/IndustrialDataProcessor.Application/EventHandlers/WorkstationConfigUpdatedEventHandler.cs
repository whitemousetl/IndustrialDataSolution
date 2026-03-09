using IndustrialDataProcessor.Application.Events;
using IndustrialDataProcessor.Application.Services;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Application.EventHandlers;

/// <summary>
/// 当接收到工作站配置更新事件时，执行缓存清理和连接重置
/// </summary>
public class WorkstationConfigUpdatedEventHandler(
    IConnectionManager connectionManager, 
    ICollectionTaskManager taskManager, 
    ILogger<WorkstationConfigUpdatedEvent> logger,
    IDataPublishServerManager dataPublishServerManager) : INotificationHandler<WorkstationConfigUpdatedEvent>
{
    private readonly IConnectionManager _connectionManager = connectionManager;
    private readonly ICollectionTaskManager _taskManager = taskManager;
    private readonly ILogger<WorkstationConfigUpdatedEvent> _logger = logger;
    private readonly IDataPublishServerManager _dataPublishServerManager = dataPublishServerManager;

    public async Task Handle(WorkstationConfigUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("收到配置更新事件，准备清理旧的连接缓存。时间: {Time}", notification.UpdatedTime);

        // 1. 关闭底层的旧物理连接（避免地址占用的冲突）调用 ConnectionManager 将所有的 TCP/串口 连接断开
        await _connectionManager.ClearAllConnectionsAsync();

        // 2. 命令任务管理器，杀掉所有旧线程，重新去数据库读最新配置并启动新线程
        await _taskManager.StartOrRestartAllTasksAsync(cancellationToken);

        // 3. 杀掉并基于最新配置重新生成 数据发布服务器 
        await _dataPublishServerManager.StartOrRestartServerAsync();

        _logger.LogInformation("旧连接清理完成，下一次轮询将使用全新配置建立连接。");
    }
}
