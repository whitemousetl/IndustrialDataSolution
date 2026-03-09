using IndustrialDataProcessor.Domain.Enums;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;

/// <summary>
/// 变量信息
/// </summary>
public class ParameterConfigDto
{
    /// <summary>
    /// 参数名，必须存在
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 地址, 虚拟点固定地址VirtualPoint,必须存在
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 是否监控，必须存在，如果Json不存在或错误，默认false
    /// </summary>
    public bool IsMonitor { get; set; } = false;

    /// <summary>
    /// 站号,现场差异，非必须存在
    /// </summary>
    public string? StationNo { get; set; } = string.Empty;

    /// <summary>
    /// 协议格式,解析或生成格式，大端序小端序,现场差异，非必须存在
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DomainDataFormat? DataFormat { get; set; }

    /// <summary>
    /// 偏移量，地址从0开始？现场差异，非必须存在
    /// </summary>
    public bool? AddressStartWithZero { get; set; }

    /// <summary>
    /// 仪表类型，CJT188专用,现场差异，非必须存在
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InstrumentType? InstrumentType { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataType? DataType { get; set; }

    /// <summary>
    /// 长度,非必须存在
    /// </summary>
    public ushort? Length { get; set; }

    /// <summary>
    /// 默认值,非必须存在
    /// </summary>
    public string? DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// 采集周期,非必须存在
    /// </summary>
    public int? Cycle { get; set; }

    /// <summary>
    /// 正表达式，一元一次方程，进制转换，虚拟点计算,非必须存在
    /// </summary>
    public string? PositiveExpression { get; set; } = string.Empty;

    /// <summary>
    /// 最小值,非必须存在
    /// </summary>
    public string? MinValue { get; set; } = string.Empty;

    /// <summary>
    /// 最大值,非必须存在
    /// </summary>
    public string? MaxValue { get; set; } = string.Empty;

    /// <summary>
    /// 写入才有值,非必须存在
    /// </summary>
    public string? Value { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProtocolType ProtocolType { get; set; }

    public string EquipmentId { get; set; } = string.Empty;
}
