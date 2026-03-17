using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;

/// <summary>
/// 参数/变量配置DTO
/// </summary>
public class ParameterConfigDto
{
    #region 基础必填字段

    /// <summary>
    /// 参数标签/名称
    /// <para>必填字段，不能为空</para>
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 地址
    /// <para>必填字段，不能为空（虚拟点固定地址为 "VirtualPoint"）</para>
    /// </summary>
    public string Address { get; set; } = string.Empty;

    #endregion

    #region 监控配置

    /// <summary>
    /// 是否监控
    /// <para>可选字段，默认值 true</para>
    /// </summary>
    public bool IsMonitor { get; set; } = true;

    #endregion

    #region 协议特性驱动的动态字段

    /// <summary>
    /// 站号/通讯地址
    /// <para>条件必填：根据 ProtocolType 的 requireStationNo 特性判断是否必填</para>
    /// </summary>
    public string? StationNo { get; set; }

    /// <summary>
    /// 数据格式/字节序（大端序/小端序）
    /// <para>条件必填：根据 ProtocolType 的 requireDataFormat 特性判断是否必填</para>
    /// </summary>
    public DomainDataFormat? DataFormat { get; set; }

    /// <summary>
    /// 地址从0开始
    /// <para>条件必填：根据 ProtocolType 的 requireAddressStartWithZero 特性判断是否必填</para>
    /// </summary>
    public bool? AddressStartWithZero { get; set; }

    /// <summary>
    /// 仪表类型（CJT188专用）
    /// <para>条件必填：根据 ProtocolType 的 requireInstrumentType 特性判断是否必填</para>
    /// </summary>
    public InstrumentType? InstrumentType { get; set; }

    /// <summary>
    /// 数据类型
    /// <para>条件必填：根据 ProtocolType 的 requireDataType 特性判断是否必填</para>
    /// </summary>
    public DataType? DataType { get; set; }

    /// <summary>
    /// 数据长度
    /// <para>当 DataType 为 String 时必填，默认值 10</para>
    /// </summary>
    public ushort? Length { get; set; }

    #endregion

    #region 可选配置字段

    /// <summary>
    /// 默认值
    /// <para>可选字段</para>
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 采集周期（毫秒）
    /// <para>可选字段</para>
    /// </summary>
    public int? Cycle { get; set; }

    /// <summary>
    /// 正表达式（支持一元一次方程、进制转换、虚拟点计算）
    /// <para>可选字段</para>
    /// </summary>
    public string? PositiveExpression { get; set; }

    /// <summary>
    /// 最小值
    /// <para>可选字段，用于范围验证</para>
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// <para>可选字段，用于范围验证，MinValue 不能大于 MaxValue</para>
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// 写入值
    /// <para>可选字段，仅在写入操作时才有值</para>
    /// </summary>
    public string? Value { get; set; }

    #endregion

    #region 上下文关联字段

    /// <summary>
    /// 协议类型
    /// <para>由父级 EquipmentConfigDto 传递，用于验证器中的上下文信息</para>
    /// </summary>
    public ProtocolType ProtocolType { get; set; }

    /// <summary>
    /// 所属设备Id
    /// <para>由父级 EquipmentConfigDto 传递，用于验证器中的上下文信息</para>
    /// </summary>
    public string EquipmentId { get; set; } = string.Empty;

    #endregion
}
