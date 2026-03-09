using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;
/// <summary>
/// 设备信息
/// </summary>
public class EquipmentConfig
{
    /// <summary>
    /// 设备Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 该设备是否采集，必须存在
    /// </summary>
    public bool IsCollect { get; set; }

    /// <summary>
    /// 设备名称，非必须存在
    /// </summary>
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型，必须存在
    /// </summary>
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

    /// <summary>
    /// 变量信息列表，必须存在
    /// </summary>
    public List<ParameterConfig>? Parameters { get; set; } = [];
}
