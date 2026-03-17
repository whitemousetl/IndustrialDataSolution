using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Exceptions;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;
using IndustrialDataProcessor.Infrastructure.Persistence.Mappers;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.Repositories;

/// <summary>
/// 工作站配置持久化仓储实现
/// <para>负责工作站配置实体的数据库 CRUD 操作</para>
/// </summary>
public class WorkstationConfigPersistenceRepository : IWorkstationConfigPersistenceRepository
{
    private readonly ISqlSugarClient _db;

    public WorkstationConfigPersistenceRepository(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db), "SqlSugar数据库客户端不能为空");
    }

    /// <inheritdoc/>
    public async Task AddAsync(WorkstationConfigEntity config, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var po = WorkstationConfigMapper.ToPo(config);
        var rows = await _db.Insertable(po).ExecuteCommandAsync(token);

        if (rows == 0)
        {
            throw new InfrastructureException("数据库写入失败");
        }
    }

    /// <inheritdoc/>
    public async Task<WorkstationConfigEntity?> GetLatestAsync(CancellationToken token = default)
    {
        var po = await _db.Queryable<WorkstationConfigPo>()
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync(token);

        return po == null ? null : WorkstationConfigMapper.ToDomain(po);
    }
}
