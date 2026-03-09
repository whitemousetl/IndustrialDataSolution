using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 波特率类型
/// </summary>
public enum BaudRateType
{
    /// <summary>
    /// 300
    /// </summary>
    [Description("300")]
    B300 = 300,

    /// <summary>
    /// 600
    /// </summary>
    [Description("600")]
    B600 = 600,

    /// <summary>
    /// 1200
    /// </summary>
    [Description("1200")]
    B1200 = 1200,

    /// <summary>
    /// 2400
    /// </summary>
    [Description("2400")]
    B2400 = 2400,

    /// <summary>
    /// 4800
    /// </summary>
    [Description("4800")]
    B4800 = 4800,

    /// <summary>
    /// 9600
    /// </summary>
    [Description("9600")]
    B9600 = 9600,

    /// <summary>
    /// 14400
    /// </summary>
    [Description("14400")]
    B14400 = 14400,

    /// <summary>
    /// 19200
    /// </summary>
    [Description("19200")]
    B19200 = 19200,

    /// <summary>
    /// 38400
    /// </summary>
    [Description("38400")]
    B38400 = 38400,

    /// <summary>
    /// 56000
    /// </summary>
    [Description("56000")]
    B56000 = 56000,

    /// <summary>
    /// 57600
    /// </summary>
    [Description("57600")]
    B57600 = 57600,

    /// <summary>
    /// 115200
    /// </summary>
    [Description("115200")]
    B115200 = 115200,

    /// <summary>
    /// 128000
    /// </summary>
    [Description("128000")]
    B128000 = 128000,

    /// <summary>
    /// 230400
    /// </summary>
    [Description("230400")]
    B230400 = 230400,

    /// <summary>
    /// 256000
    /// </summary>
    [Description("256000")]
    B256000 = 256000,
}