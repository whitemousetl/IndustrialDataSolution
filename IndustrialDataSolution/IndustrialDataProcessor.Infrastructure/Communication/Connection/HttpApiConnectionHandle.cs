using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Infrastructure.Communication.Connection;

/// <summary>
/// HTTP API 连接句柄
/// 封装 HttpClient 及相关配置信息
/// </summary>
public class HttpApiConnectionHandle : IConnectionHandle
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// API访问地址
    /// </summary>
    public string AccessApiString { get; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public RequestMethod RequestMethod { get; }

    /// <summary>
    /// 账号（可选）
    /// </summary>
    public string? Account { get; }

    /// <summary>
    /// 密码（可选）
    /// </summary>
    public string? Password { get; }

    /// <summary>
    /// 网关（可选）
    /// </summary>
    public string? Gateway { get; }

    /// <summary>
    /// 创建 HTTP API 连接句柄
    /// </summary>
    public HttpApiConnectionHandle(
        HttpClient httpClient,
        string accessApiString,
        RequestMethod requestMethod,
        string? account = null,
        string? password = null,
        string? gateway = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        AccessApiString = accessApiString ?? throw new ArgumentNullException(nameof(accessApiString));
        RequestMethod = requestMethod;
        Account = account;
        Password = password;
        Gateway = gateway;
    }

    /// <summary>
    /// 获取底层 HttpClient
    /// </summary>
    public TConnection GetRawConnection<TConnection>() where TConnection : class
    {
        if (_httpClient is not TConnection typedConn)
            throw new InvalidCastException($"底层连接类型不匹配，期望 {typeof(TConnection).Name}，实际为 {_httpClient.GetType().Name}");

        return typedConn;
    }

    /// <summary>
    /// 获取通道锁（保证同一连接不会并发请求）
    /// </summary>
    public async Task<IDisposable> AcquireLockAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        return new Releaser(_semaphore);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        _semaphore.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 辅助类：用于 using 语法自动释放锁
    /// </summary>
    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = semaphore;
        public void Dispose() => _semaphore.Release();
    }
}
