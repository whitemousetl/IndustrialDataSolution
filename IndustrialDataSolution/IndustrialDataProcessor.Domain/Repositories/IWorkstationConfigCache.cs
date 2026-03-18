using IndustrialDataProcessor.Domain.Workstation.Configs;

namespace IndustrialDataProcessor.Domain.Repositories;

/// <summary>
/// 工作站配置缓存接口
/// <para>提供线程安全的配置缓存，减少重复数据库查询</para>
/// </summary>
public interface IWorkstationConfigCache
{
    /// <summary>
    /// 获取当前缓存的配置
    /// </summary>
    /// <returns>缓存的配置，若未缓存则返回null</returns>
    WorkstationConfig? GetCurrentConfig();

    /// <summary>
    /// 异步获取配置（如果缓存不存在则从仓储加载）
    /// </summary>
    /// <param name="repository">配置仓储</param>
    /// <param name="token">取消令牌</param>
    /// <returns>工作站配置</returns>
    Task<WorkstationConfig?> GetOrLoadAsync(IWorkstationConfigRepository repository, CancellationToken token);

    /// <summary>
    /// 更新缓存中的配置
    /// </summary>
    /// <param name="config">新的配置</param>
    void UpdateCache(WorkstationConfig? config);

    /// <summary>
    /// 清空缓存
    /// </summary>
    void ClearCache();

    /// <summary>
    /// 获取缓存的版本标识（用于判断是否需要刷新）
    /// </summary>
    long Version { get; }

    /// <summary>
    /// 缓存最后更新时间
    /// </summary>
    DateTime? LastUpdated { get; }
}
