using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Application.Services;

public class CollectionTaskManager(IServiceScopeFactory scopeFactory, ILogger<CollectionTaskManager> logger) : ICollectionTaskManager
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<CollectionTaskManager> _logger = logger;

    // 用于控制当前这一批正在运行的任务的 TokenSource
    private CancellationTokenSource? _currentCts;
    // 保护重启操作不能并发执行
    private readonly SemaphoreSlim _restartLock = new(1, 1);

    // 宿主程序退出时的最终 Token
    private CancellationToken _appStoppingToken;

    public async Task StartOrRestartAllTasksAsync(CancellationToken appStoppingToken = default)
    {
        // 记录宿主的 token，方便之后手动重启时使用
        if (appStoppingToken != default)
            _appStoppingToken = appStoppingToken;

        await _restartLock.WaitAsync(appStoppingToken);

        try
        {
            _logger.LogInformation("准备启动/重启所有采集任务...");

            // 1. 如果之前有任务在跑，发信号让它们取消
            if (_currentCts != null && !_currentCts.IsCancellationRequested)
            {
                _logger.LogInformation("正在取消上一次的旧采集任务...");
                await _currentCts.CancelAsync();

                // 这里我们可以加上一小段延时，确保旧线程已经安全让出了句柄和资源
                await Task.Delay(1000, appStoppingToken);
                _currentCts.Dispose();
            }

            // 2. 创建一个新的令牌，只要宿主没死，或者没被手动取消，就一直是有效的
            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(_appStoppingToken);

            // 3. 创建一个短暂的 scope 来解析 AppService (因为它依赖 DbContext 是 Scoped 的)
            using var scope = _scopeFactory.CreateScope();
            var appService = scope.ServiceProvider.GetRequiredService<IDataCollectionAppService>();

            // 4. 让 AppService 开出所有后台线程。
            // 注意：这里不需要 await 这个方法，因为它内部是用 _ = Task.Run 丢到后台的
            await appService.StartAllProtocolCollectionTasksAsync(_currentCts.Token);

            _logger.LogInformation("所有新采集任务已在后台下发完毕。");
        }
        finally
        {
            _restartLock.Release();
        }
    }
}
