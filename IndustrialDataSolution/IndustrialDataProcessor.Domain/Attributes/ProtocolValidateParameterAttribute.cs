namespace IndustrialDataProcessor.Domain.Attributes;

/// <summary>
/// ProtocolParameterAttribute 是一个自定义特性（Attribute），它的作用是给协议类型（ProtocolType 枚举的每个值）“打标签”，标记这个协议在参数校验时需要哪些字段（比如是否必须有站号、数据格式、数据类型等）。
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ProtocolValidateParameterAttribute : Attribute
{
    public bool RequireStationNo { get; set; }
    public bool RequireDataType { get; set; }
    public bool RequireDataFormat { get; set; }
    public bool RequireAddressStartWithZero { get; set; }
    public bool RequireInstrumentType { get; set; }
    // 可扩展更多参数
    public ProtocolValidateParameterAttribute(bool requireStationNo = false,
        bool requireDataFormat = false,
        bool requireDataType = false,
        bool requireAddressStartWithZero = false,
        bool requireInstrumentType = false)
    {
        RequireStationNo = requireStationNo;
        RequireDataFormat = requireDataFormat;
        RequireDataType = requireDataType;
        RequireAddressStartWithZero = requireAddressStartWithZero;
        RequireInstrumentType = requireInstrumentType;
    }
}
