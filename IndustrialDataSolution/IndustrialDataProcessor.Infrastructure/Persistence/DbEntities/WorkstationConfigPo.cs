using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;

/// <summary>
/// 工作站配置持久化对象
/// </summary>
[SugarTable("workstation_config")]
public class WorkstationConfigPo
{
    [SugarColumn(ColumnName = "created_at", IsPrimaryKey = true)]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "json_content", ColumnDataType = "jsonb", IsNullable = false, InsertSql = "CAST(@json_content AS jsonb)")]
    public string JsonContent { get; set; } = string.Empty;
}
