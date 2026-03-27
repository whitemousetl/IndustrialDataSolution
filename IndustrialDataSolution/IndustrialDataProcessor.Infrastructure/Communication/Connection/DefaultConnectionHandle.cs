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
        if (_rawConnection != null)
        {
            // HSL 的网络通信类（OmronFinsNet 等）在 NetworkDoubleBase 上定义了 ConnectClose，
            // 但该方法不在 IReadWriteNet 接口上。
            // 用反射调用可防止编译失败，同时确保 TCP 连接被正确关闭并释放设备端连接槽
            var closeMethod = _rawConnection.GetType().GetMethod("ConnectClose", Type.EmptyTypes);
            if (closeMethod != null)
            {
                try { closeMethod.Invoke(_rawConnection, null); } catch { /* 忽略关闭时的异常，避免掩盖主要错误 */ }
            }
            else if (_rawConnection is IDisposable disposable)
            {
                disposable.Dispose();
            }
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