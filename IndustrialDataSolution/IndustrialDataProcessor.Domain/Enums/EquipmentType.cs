using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 设备类型
/// </summary>
public enum EquipmentType
{
    /// <summary>
    /// 设备
    /// </summary>
    [Description("设备")]
    Equipment = 0,

    /// <summary>
    /// 仪表
    /// </summary>
    [Description("仪表")]
    Instrument = 1,
}
