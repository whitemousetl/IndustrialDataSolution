using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 数据类型
/// </summary>
public enum DataType
{
    /// <summary>
    /// Bool
    /// </summary>
    [Description("Bool")]
    Bool = 0,

    /// <summary>
    /// UShort
    /// </summary>
    [Description("UShort")]
    UShort = 1,

    /// <summary>
    /// Short
    /// </summary>
    [Description("Short")]
    Short = 2,

    /// <summary>
    /// UInt
    /// </summary>
    [Description("UInt")]
    UInt = 3,

    /// <summary>
    /// Int
    /// </summary>
    [Description("Int")]
    Int = 4,

    /// <summary>
    /// ULong
    /// </summary>
    [Description("ULong")]
    ULong = 5,

    /// <summary>
    /// Long
    /// </summary>
    [Description("Long")]
    Long = 6,

    /// <summary>
    /// Float
    /// </summary>
    [Description("Float")]
    Float = 7,

    /// <summary>
    /// Double
    /// </summary>
    [Description("Double")]
    Double = 8,

    /// <summary>
    /// String
    /// </summary>
    [Description("String")]
    String = 9,
}