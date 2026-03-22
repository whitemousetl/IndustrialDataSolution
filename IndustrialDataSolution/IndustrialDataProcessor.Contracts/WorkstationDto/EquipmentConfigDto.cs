using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Contracts.WorkstationDto;

/// <summary>
/// 设备配置DTO
/// </summary>
public class EquipmentConfigDto
{
    /// <summary>
    /// 设备Id
    /// <para>必填字段，不能为空</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 该设备是否采集
    /// <para>可选字段，默认值 true</para>
    /// </summary>
    public bool IsCollect { get; set; } = true;

    /// <summary>
    /// 设备名称
    /// <para>可选字段</para>
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 设备类型
    /// <para>必填字段，必须是合法的 EquipmentType 枚举值，默认值 Equipment</para>
    /// </summary>
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

    /// <summary>
    /// 变量/参数信息列表
    /// <para>可选字段，为空时默认为空集合</para>
    /// </summary>
    public List<ParameterConfigDto>? Parameters { get; set; } = [];

    /// <summary>
    /// 协议类型
    /// <para>由父级 ProtocolConfigDto 传递，用于验证器中的上下文信息</para>
    /// </summary>
    public ProtocolType ProtocolType { get; set; }
}
