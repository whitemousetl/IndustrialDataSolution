using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Diagnostics.CodeAnalysis;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.DbEntities;

/// <summary>
/// 设备实时数据表实体（对应TimescaleDB超表）
/// </summary>
[ExcludeFromCodeCoverage]
[SugarTable("equipment_real_time_data")] // 指定数据库表名
public class EquipmentData
{
    /// <summary>
    /// 时序核心字段（TIMESTAMPTZ，带时区）
    /// </summary>
    [SugarColumn(ColumnName = "time", IsPrimaryKey = true)] // TimescaleDB超表无需自增主键，time作为分区键
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
    /// 方式1：用JObject（推荐，支持灵活的JSON操作）
    /// </summary>
    [SugarColumn(ColumnName = "values", ColumnDataType = "jsonb", IsJson = true)] // 显式指定数据库类型为jsonb
    public string Values { get; set; } = string.Empty;
}