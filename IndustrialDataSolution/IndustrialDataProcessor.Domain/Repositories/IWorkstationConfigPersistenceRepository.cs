using IndustrialDataProcessor.Domain.Entities;

namespace IndustrialDataProcessor.Domain.Repositories;

/// <summary>
/// 工作站配置持久化仓储接口
/// <para>负责工作站配置实体的数据库存储操作</para>
/// </summary>
public interface IWorkstationConfigPersistenceRepository
{
    /// <summary>
    /// 获取最新的工作站配置实体
    /// </summary>
    /// <param name="token">取消令牌</param>
    /// <returns>最新的配置实体，若不存在则返回 null</returns>
    Task<WorkstationConfigEntity?> GetLatestAsync(CancellationToken token);

    /// <summary>
    /// 添加工作站配置实体到数据库
    /// </summary>
    /// <param name="config">工作站配置实体</param>
    /// <param name="token">取消令牌</param>
    Task AddAsync(WorkstationConfigEntity config, CancellationToken token);
}
