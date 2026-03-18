using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IndustrialDataProcessor.Infrastructure.Persistence.Repositories;

/// <summary>
/// 工作站配置缓存实现
/// <para>使用读写锁确保线程安全，支持并发读取</para>
/// </summary>
public class WorkstationConfigCache : IWorkstationConfigCache
{
    // 使用ReaderWriterLockSlim实现读写分离的线程安全
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private WorkstationConfig? _cachedConfig;
    private long _version;
    private DateTime? _lastUpdated;

    private readonly ILogger<WorkstationConfigCache> _logger;

    // 用于防止并发加载的同步机制
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading = false;

    public WorkstationConfigCache(ILogger<WorkstationConfigCache> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public WorkstationConfig? GetCurrentConfig()
    {
        _lock.EnterReadLock();
        try
        {
            return _cachedConfig;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public async Task<WorkstationConfig?> GetOrLoadAsync(IWorkstationConfigRepository repository, CancellationToken token)
    {
        // 1. 先尝试读取缓存（无锁快速路径）
        _lock.EnterReadLock();
        try
        {
            if (_cachedConfig != null)
            {
                _logger.LogDebug("缓存命中，返回缓存的配置 [Version: {Version}]", _version);
                return _cachedConfig;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // 2. 缓存未命中，使用信号量防止并发加载
        await _loadSemaphore.WaitAsync(token);
        try
        {
            // 双重检查：等待信号量后再次检查缓存
            _lock.EnterReadLock();
            try
            {
                if (_cachedConfig != null)
                {
                    _logger.LogDebug("等待后缓存命中，返回缓存的配置 [Version: {Version}]", _version);
                    return _cachedConfig;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // 3. 从仓储加载
            _logger.LogInformation("缓存未命中，从数据库加载配置...");
            _isLoading = true;

            try
            {
                var config = await repository.GetLatestParsedConfigAsync(token);

                // 4. 更新缓存
                _lock.EnterWriteLock();
                try
                {
                    _cachedConfig = config;
                    _version = DateTime.UtcNow.Ticks;
                    _lastUpdated = DateTime.UtcNow;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _logger.LogInformation("配置已加载并缓存 [Version: {Version}], Protocols: {ProtocolCount}",
                    _version, config?.Protocols?.Count ?? 0);

                return config;
            }
            finally
            {
                _isLoading = false;
            }
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void UpdateCache(WorkstationConfig? config)
    {
        _lock.EnterWriteLock();
        try
        {
            _cachedConfig = config;
            _version = DateTime.UtcNow.Ticks;
            _lastUpdated = DateTime.UtcNow;

            _logger.LogInformation("缓存已更新 [Version: {Version}], Protocols: {ProtocolCount}",
                _version, config?.Protocols?.Count ?? 0);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _lock.EnterWriteLock();
        try
        {
            _cachedConfig = null;
            _version = 0;
            _lastUpdated = null;

            _logger.LogInformation("缓存已清空");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public long Version
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _version;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc />
    public DateTime? LastUpdated
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _lastUpdated;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// 检查是否正在加载中
    /// </summary>
    public bool IsLoading => _isLoading;
}
