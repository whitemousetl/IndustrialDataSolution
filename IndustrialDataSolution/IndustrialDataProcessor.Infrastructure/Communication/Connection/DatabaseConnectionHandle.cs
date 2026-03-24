using IndustrialDataProcessor.Domain.Communication.IConnection;
using SqlSugar;
using System.Collections.Concurrent;
using System.Data;

namespace IndustrialDataProcessor.Infrastructure.Communication.Connection;

/// <summary>
/// 数据库协议连接句柄
/// 封装 SqlSugar 客户端及协议级 QuerySqlString
/// 行级查询缓存属于连接句柄，不同协议配置之间完全隔离
/// </summary>
public class DatabaseConnectionHandle : IConnectionHandle
{
    private readonly ISqlSugarClient _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // 缓存连接句柄级别，不同协议配置实例间正常隔离，解决单例驱动共享时的缓存键冲突问题
    private readonly ConcurrentDictionary<string, RowCacheEntry> _rowCache = new();
    private const int DefaultCacheExpiryMs = 1000;

    /// <summary>
    /// 协议配置的查询 SQL（对应 DatabaseInterfaceConfig.QuerySqlString）
    /// 驱动执行此 SQL 后，按 ParameterConfig.Address（列名）提取每个点的値
    /// </summary>
    public string QuerySqlString { get; }

    public DatabaseConnectionHandle(ISqlSugarClient client, string querySqlString)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        QuerySqlString = querySqlString ?? string.Empty;
    }

    public TConnection GetRawConnection<TConnection>() where TConnection : class
    {
        if (_client is not TConnection typed)
            throw new InvalidCastException(
                $"底层连接类型不匹配，期望 {typeof(TConnection).Name}，实际为 {_client.GetType().Name}");
        return typed;
    }

    public async Task<IDisposable> AcquireLockAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        return new Releaser(_semaphore);
    }

    /// <summary>
    /// 执行 SQL 查询并缓存首行结果（缓存有效期内不重复查库）
    /// 缓存属于此连接句柄，不同协议配置的缓存完全独立
    /// </summary>
    public async Task<Dictionary<string, object?>?> GetOrExecuteQueryAsync(string sql)
    {
        if (_rowCache.TryGetValue(sql, out var cached) && !cached.IsExpired)
            return cached.Row;

        var dataTable = await _client.Ado.GetDataTableAsync(sql);

        if (dataTable.Rows.Count == 0)
        {
            _rowCache[sql] = RowCacheEntry.Empty(DefaultCacheExpiryMs);
            return null;
        }

        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DataColumn col in dataTable.Columns)
        {
            var rawValue = dataTable.Rows[0][col];
            row[col.ColumnName] = rawValue == DBNull.Value ? null : rawValue;
        }

        _rowCache[sql] = RowCacheEntry.WithRow(row, DefaultCacheExpiryMs);
        return row;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        _semaphore.Dispose();
        _rowCache.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = semaphore;
        public void Dispose() => _semaphore.Release();
    }

    private sealed class RowCacheEntry
    {
        public Dictionary<string, object?>? Row { get; private init; }
        public DateTime ExpiryTime { get; private init; }
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;

        private RowCacheEntry() { }

        public static RowCacheEntry WithRow(Dictionary<string, object?> row, int expiryMs) =>
            new() { Row = row, ExpiryTime = DateTime.UtcNow.AddMilliseconds(expiryMs) };

        public static RowCacheEntry Empty(int expiryMs) =>
            new() { Row = null, ExpiryTime = DateTime.UtcNow.AddMilliseconds(expiryMs) };
    }
}
