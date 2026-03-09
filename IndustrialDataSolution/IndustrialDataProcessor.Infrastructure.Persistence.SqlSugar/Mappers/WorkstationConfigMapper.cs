using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.DbEntities;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.Mappers;

public static class WorkstationConfigMapper
{
    public static WorkstationConfigEntity ToDomain(WorkstationConfigPo po)
    {
        return new WorkstationConfigEntity
        {
            CreateAt = po.CreatedAt,
            JsonContent = po.JsonContent
        };
    }

    public static WorkstationConfigPo ToPo(WorkstationConfigEntity entity)
    {
        return new WorkstationConfigPo
        {
            CreatedAt = entity.CreateAt,
            JsonContent = entity.JsonContent
        };
    }
}
