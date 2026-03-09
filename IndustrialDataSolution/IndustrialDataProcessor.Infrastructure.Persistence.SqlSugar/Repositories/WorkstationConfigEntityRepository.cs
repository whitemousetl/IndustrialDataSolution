using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Exceptions;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.DbEntities;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.Mappers;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.Repositories;

public class WorkstationConfigEntityRepository(ISqlSugarClient db) : IWorkstationConfigEntityRepository
{
    private readonly ISqlSugarClient _db = db ?? throw new ArgumentNullException(nameof(db), "SqlSugar数据库客户端不能为空");
    public async Task AddAsync(WorkstationConfigEntity config, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var po = WorkstationConfigMapper.ToPo(config);

        var rows = await _db.Insertable(po).ExecuteCommandAsync(token);

        if (rows == 0) throw new InfrastructureException("数据库写入失败");
    }

    public async Task<WorkstationConfigEntity?> GetLatestAsync(CancellationToken token = default)
    {
        var po = await _db.Queryable<WorkstationConfigPo>()
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstAsync(token);

        return po == null ? null : WorkstationConfigMapper.ToDomain(po);
    }
}