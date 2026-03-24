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

    // 缓存属于连接句柄级别，不同协议配置实例间完全隔离
    // 以 SQL 字符串为键，缓存该 SQL 查询的完整结果集（所有行），支持单行和多行查询
    private readonly ConcurrentDictionary<string, TableCacheEntry> _tableCache = new();
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
    /// 执行 SQL 查询并缓存完整结果集（缓存有效期内不重复查库）
    /// 同时支持单行查询和多行查询（例如：一条 SQL 返回多台设备的数据）
    /// 缓存属于此连接句柄，不同协议配置的缓存完全独立
    /// </summary>
    /// <returns>所有行的列表，每行是「列名→值」字典；SQL 无数据时返回 null</returns>
    public async Task<List<Dictionary<string, object?>>?> GetAllRowsAsync(string sql)
    {
        if (_tableCache.TryGetValue(sql, out var cached) && !cached.IsExpired)
            return cached.Rows;

        var dataTable = await _client.Ado.GetDataTableAsync(sql);

        if (dataTable.Rows.Count == 0)
        {
            _tableCache[sql] = TableCacheEntry.Empty(DefaultCacheExpiryMs);
            return null;
        }

        var rows = new List<Dictionary<string, object?>>(dataTable.Rows.Count);
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in dataTable.Columns)
            {
                var rawValue = dataRow[col];
                row[col.ColumnName] = rawValue == DBNull.Value ? null : rawValue;
            }
            rows.Add(row);
        }

        _tableCache[sql] = TableCacheEntry.WithRows(rows, DefaultCacheExpiryMs);
        return rows;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        _semaphore.Dispose();
        _tableCache.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = semaphore;
        public void Dispose() => _semaphore.Release();
    }

    private sealed class TableCacheEntry
    {
        public List<Dictionary<string, object?>>? Rows { get; private init; }
        public DateTime ExpiryTime { get; private init; }
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;

        private TableCacheEntry() { }

        public static TableCacheEntry WithRows(List<Dictionary<string, object?>> rows, int expiryMs) =>
            new() { Rows = rows, ExpiryTime = DateTime.UtcNow.AddMilliseconds(expiryMs) };

        public static TableCacheEntry Empty(int expiryMs) =>
            new() { Rows = null, ExpiryTime = DateTime.UtcNow.AddMilliseconds(expiryMs) };
    }
}
