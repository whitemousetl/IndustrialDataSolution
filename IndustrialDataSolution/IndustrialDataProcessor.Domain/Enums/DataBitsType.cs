using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 数据位
/// </summary>
public enum DataBitsType
{
    /// <summary>
    /// 7
    /// </summary>
    [Description("7")]
    D7 = 7,

    /// <summary>
    /// 8
    /// </summary>
    [Description("8")]
    D8 = 8,
}