using IndustrialDataProcessor.Domain.Workstation.Configs;

namespace IndustrialDataProcessor.Domain.Communication.IConnection;

public interface IConnectionManager
{
    /// <summary>
    /// 根据协议配置获取或创建连接句柄 (包含自动重连逻辑)
    /// </summary>
    /// <param name="config">协议配置</param>
    /// <param name="token">token</param>
    Task<IConnectionHandle> GetOrCreateConnectionAsync(ProtocolConfig config, CancellationToken token);

    /// <summary>
    /// 清除所有已建立的连接，并销毁他们 (常用于配置发生变更时)
    /// </summary>
    Task ClearAllConnectionsAsync();
}
