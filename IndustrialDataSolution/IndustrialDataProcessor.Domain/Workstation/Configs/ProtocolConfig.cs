using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;

/// <summary>
/// 协议信息
/// </summary>
public abstract class ProtocolConfig
{
    /// <summary>
    /// 协议Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 接口类型，必须存在
    /// </summary>
    public abstract InterfaceType InterfaceType { get; }

    /// <summary>
    /// 协议类型，必须存在
    /// </summary>
    public ProtocolType ProtocolType { get; set; }

    /// <summary>
    /// 通讯延时,默认1000ms，有默认值，非必须存在
    /// </summary>
    public int CommunicationDelay { get; set; } = 1000;

    /// <summary>
    /// 接收超时,默认500ms，有默认值，非必须存在
    /// </summary>
    public int ReceiveTimeOut { get; set; } = 500;

    /// <summary>
    /// 连接超时，默认500ms，有默认值，非必须存在
    /// </summary>
    public int ConnectTimeOut { get; set; } = 500;

    /// <summary>
    /// 账号，非必须存在
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密码，非必须存在
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 备注，非必须存在
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 可选参数，非必须存在
    /// </summary>
    public string AdditionalOptions { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息列表，必须存在
    /// </summary>
    public List<EquipmentConfig> Equipments { get; set; } = [];
}