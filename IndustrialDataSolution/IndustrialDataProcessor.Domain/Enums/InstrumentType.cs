using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 仪表类型
/// </summary>
public enum InstrumentType
{
    /// <summary>
    /// 冷水水表0x10【水表类】
    /// </summary>
    [Description("冷水水表")]
    ColdWater = 16,

    /// <summary>
    /// 生活热水水表0x11【水表类】
    /// </summary>
    [Description("生活热水水表")]
    DomesticHotWater = 17,    // 生活热水水表

    /// <summary>
    /// 直饮水水表0x12【水表类】
    /// </summary>
    [Description("直饮水水表")]
    DrinkingWater = 18,

    /// <summary>
    /// 中水水表0x13【水表类】
    /// </summary>
    [Description("中水水表")]
    ReclaimedWater = 19,

    /// <summary>
    /// 热量0x20【热量表类】
    /// </summary>
    [Description("热量表")]
    HeatMeter = 32,

    /// <summary>
    /// 冷量0x21【热量表类】
    /// </summary>
    [Description("冷量表")]
    CoolingMeter = 33,

    /// <summary>
    /// 燃气表0x30【其他】
    /// </summary>
    [Description("燃气表")]
    GasMeter = 48,

    /// <summary>
    /// 电度表0x40【其他】
    /// </summary>
    [Description("电度表")]
    ElectricityMeter = 64,
}
