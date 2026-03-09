using HslCommunication.Core;
using IndustrialDataProcessor.Domain.Communication.IConnection;

namespace IndustrialDataProcessor.Infrastructure.Communication.Connection;

public class DefaultConnectionHandle : IConnectionHandle
{
    private readonly object _rawConnection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public DefaultConnectionHandle(object rawConnection)
    {
        _rawConnection = rawConnection;
    }

    public async Task<IDisposable> AcquireLockAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        return new Releaser(_semaphore);
    }

    public async ValueTask DisposeAsync()
    {
        // 释放底层 HSL 连接
        if (_rawConnection is IReadWriteNet net)
        {
            //net.ConnectClose();
        }
        else if (_rawConnection is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _semaphore.Dispose();
    }

    public TConnection GetRawConnection<TConnection>() where TConnection : class
    {
        if (_rawConnection is not TConnection typedConn)
            throw new InvalidCastException($"底层连接类型不匹配，期望 {typeof(TConnection).Name}，实际为 {_rawConnection.GetType().Name}");

        return typedConn;
    }

    // 辅助类：用于 using 语法自动释放锁
    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = semaphore;
        public void Dispose() => _semaphore.Release();
    }
}