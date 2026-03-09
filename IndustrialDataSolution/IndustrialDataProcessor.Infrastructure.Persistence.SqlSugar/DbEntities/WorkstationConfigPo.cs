using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.DbEntities;

[SugarTable("workstation_config")]
public class WorkstationConfigPo
{
    [SugarColumn(ColumnName = "created_at", IsPrimaryKey = true)]
    public DateTimeOffset CreatedAt { get; set; }

    [SugarColumn(ColumnName = "json_content", ColumnDataType = "jsonb", IsNullable = false, InsertSql = "CAST(@json_content AS jsonb)")]
    public string JsonContent { get; set; } = string.Empty;
}

