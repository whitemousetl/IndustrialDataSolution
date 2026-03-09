using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
/// <summary>
/// 串口协议信息
/// </summary>
public class SerialPortConfig : ProtocolConfig
{
    /// <summary>
    /// 接口类型,必须存在
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.COM;

    /// <summary>
    /// 串口名称,必须存在
    /// </summary>
    public string SerialPortName { get; set; } = string.Empty;

    /// <summary>
    /// 波特率,必须存在
    /// </summary>
    public BaudRateType? BaudRate { get; set; }

    /// <summary>
    /// 数据位,必须存在
    /// </summary>
    public DataBitsType? DataBits { get; set; }

    /// <summary>
    /// 校验位,必须存在
    /// </summary>
    public DomainParity? Parity { get; set; }

    /// <summary>
    /// 停止位,必须存在
    /// </summary>
    public DomainStopBits? StopBits { get; set; }
}