using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;

namespace IndustrialDataProcessor.Infrastructure.Persistence.Mappers;

/// <summary>
/// 工作站配置对象映射器
/// </summary>
public static class WorkstationConfigMapper
{
    /// <summary>
    /// 将持久化对象转换为领域实体
    /// </summary>
    public static WorkstationConfigEntity ToDomain(WorkstationConfigPo po)
    {
        return new WorkstationConfigEntity
        {
            CreatedAt = po.CreatedAt,
            JsonContent = po.JsonContent
        };
    }

    /// <summary>
    /// 将领域实体转换为持久化对象
    /// </summary>
    public static WorkstationConfigPo ToPo(WorkstationConfigEntity entity)
    {
        return new WorkstationConfigPo
        {
            CreatedAt = entity.CreatedAt,
            JsonContent = entity.JsonContent
        };
    }
}
