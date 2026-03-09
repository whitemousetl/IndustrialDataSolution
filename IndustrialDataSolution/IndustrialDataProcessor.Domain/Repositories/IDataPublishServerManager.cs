namespace IndustrialDataProcessor.Domain.Repositories;

/// <summary>
/// 管理对外发布数据的服务器生命周期（如重启、停止）
/// </summary>
public interface IDataPublishServerManager
{
    Task StartOrRestartServerAsync();
}
