using SqlSugar;
using System.Diagnostics.CodeAnalysis;

namespace IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;

/// <summary>
/// 设备实时数据表实体（对应TimescaleDB超表）
/// </summary>
[ExcludeFromCodeCoverage]
[SugarTable("equipment_real_time_data")]
public class EquipmentData
{
    /// <summary>
    /// 时序核心字段（TIMESTAMPTZ，带时区）
    /// </summary>
    [SugarColumn(ColumnName = "time", IsPrimaryKey = true)]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 本地时间（TIMESTAMP，无时区）
    /// </summary>
    [SugarColumn(ColumnName = "local_time")]
    public DateTime LocalTime { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(ColumnName = "equipment_id", Length = 50)]
    public string EquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// 设备参数（JSONB类型）
    /// </summary>
    [SugarColumn(ColumnName = "values", ColumnDataType = "jsonb", IsJson = true)]
    public string Values { get; set; } = string.Empty;
}
