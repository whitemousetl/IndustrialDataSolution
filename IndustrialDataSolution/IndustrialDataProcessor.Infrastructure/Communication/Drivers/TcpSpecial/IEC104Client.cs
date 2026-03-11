using lib60870.CS101;
using lib60870.CS104;
using System.Collections.Concurrent;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpSpecial;

/// <summary>
/// IEC 104 协议客户端
/// </summary>
public class Iec104Client : IDisposable
{
    private lib60870.CS104.Connection? _connection;
    private readonly ConcurrentDictionary<string, object> _dataCache = new();
    private readonly string _ipAddress;
    private readonly int _port;

    /// <summary>
 /// 创建 IEC 104 客户端实例
    /// </summary>
    public Iec104Client(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public void Connect()
    {
        _connection = new lib60870.CS104.Connection(_ipAddress, _port);
        // TODO: 实现实际的连接逻辑
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _connection = null;
        GC.SuppressFinalize(this);
    }
}
