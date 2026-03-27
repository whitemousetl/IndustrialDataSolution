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

    /// <summary>
    /// 使指定协议的连接失效并释放（发生协议级异常时调用）
    /// 下次 GetOrCreateConnectionAsync 将重新建立全新连接，避免使用已断开的僵尸连接
    /// </summary>
    /// <param name="protocolId">协议配置 Id</param>
    Task InvalidateConnectionAsync(string protocolId);
}
