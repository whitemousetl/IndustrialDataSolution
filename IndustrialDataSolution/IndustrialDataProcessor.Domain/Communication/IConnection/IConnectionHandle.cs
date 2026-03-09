namespace IndustrialDataProcessor.Domain.Communication.IConnection;

public interface IConnectionHandle : IAsyncDisposable
{
    /// <summary>
    /// 获取底层的通信对象 (如 MosbusTcpNet，ModbusRtu)
    /// </summary>
    /// <typeparam name="TConnection">通信对象</typeparam>
    /// <returns></returns>
    TConnection GetRawConnection<TConnection>() where TConnection : class;

    /// <summary>
    /// 获取通道锁，确保同一物理道 (特别是串口) 的读写是串行的
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<IDisposable> AcquireLockAsync(CancellationToken token);
}
