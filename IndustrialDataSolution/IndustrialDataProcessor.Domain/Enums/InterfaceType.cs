using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;
/// <summary>
/// 接口类型
/// </summary>
public enum InterfaceType
{
    /// <summary>
    /// LAN
    /// </summary>
    [Description("LAN")]
    LAN = 0,

    /// <summary>
    /// COM
    /// </summary>
    [Description("COM")]
    COM = 1,

    /// <summary>
    /// API
    /// </summary>
    [Description("API")]
    API = 2,

    /// <summary>
    /// DATABASE
    /// </summary>
    [Description("DATABASE")]
    DATABASE = 3,
}