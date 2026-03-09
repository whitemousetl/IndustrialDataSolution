using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
/// <summary>
/// 网口协议信息
/// </summary>
public class NetworkProtocolConfig : ProtocolConfig
{
    /// <summary>
    /// 接口类型,必须存在
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.LAN;

    /// <summary>
    /// IP地址,必须存在
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 端口,,必须存在
    /// </summary>
    public int ProtocolPort { get; set; }

    /// <summary>
    /// 代理网关,非必须存在
    /// </summary>
    public string Gateway { get; set; } = string.Empty;
}