using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Exceptions;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;
using IndustrialDataProcessor.Infrastructure.Persistence.Mappers;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.Repositories;

/// <summary>
/// 工作站配置实体仓储实现
/// </summary>
public class WorkstationConfigEntityRepository : IWorkstationConfigEntityRepository
{
    private readonly ISqlSugarClient _db;

    public WorkstationConfigEntityRepository(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db), "SqlSugar数据库客户端不能为空");
    }

    /// <summary>
    /// 添加工作站配置
    /// </summary>
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

    /// <summary>
    /// 获取最新的工作站配置
    /// </summary>
    public async Task<WorkstationConfigEntity?> GetLatestAsync(CancellationToken token = default)
    {
        var po = await _db.Queryable<WorkstationConfigPo>()
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync(token);

        return po == null ? null : WorkstationConfigMapper.ToDomain(po);
    }
}
