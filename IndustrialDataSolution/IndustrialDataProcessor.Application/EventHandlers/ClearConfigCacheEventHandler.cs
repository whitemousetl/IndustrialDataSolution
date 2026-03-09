using IndustrialDataProcessor.Application.Constants;
using IndustrialDataProcessor.Application.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Application.EventHandlers;
/// <summary>
/// 监听配置更新事件，负责清除内存缓存
/// </summary>
public class ClearConfigCacheEventHandler(IMemoryCache memoryCache, ILogger<ClearConfigCacheEventHandler> logger) : INotificationHandler<WorkstationConfigUpdatedEvent>
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<ClearConfigCacheEventHandler> _logger = logger;

    public Task Handle(WorkstationConfigUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // 核心动作：清除缓存
        _memoryCache.Remove(CacheKeys.LatestWorkstationConfig);

        _logger.LogInformation("接收到配置更新事件，已清除工作站配置缓存。触发时间：{UpdatedTime}", notification.UpdatedTime);

        return Task.CompletedTask;
    }
}
