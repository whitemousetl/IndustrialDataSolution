using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Attributes;

/// <summary>
/// 这个特性是用来给协议类型（ProtocolType 枚举的每个值）“打标签”，标记它属于哪种接口类型（比如 LAN、COM、API、DATABASE）。
/// 比如你有一个协议叫 ModbusTcpNet，它只能用在 LAN 接口上。你就可以在枚举值ModbusTcpNet上打标签[ProtocolInterfaceType(InterfaceType.LAN)]
/// ModbusTcpNet = 0,
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ProtocolInterfaceTypeAttribute : Attribute
{
    public InterfaceType InterfaceType { get; }
    public ProtocolInterfaceTypeAttribute(InterfaceType interfaceType)
    {
        InterfaceType = interfaceType;
    }
}
