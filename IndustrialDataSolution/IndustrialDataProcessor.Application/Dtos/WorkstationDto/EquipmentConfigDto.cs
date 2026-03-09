using IndustrialDataProcessor.Domain.Enums;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;
/// <summary>
/// 设备信息
/// </summary>
public class EquipmentConfigDto
{
    /// <summary>
    /// 设备Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 该设备是否采集，必须存在
    /// </summary>
    public bool IsCollect { get; set; } = true;

    /// <summary>
    /// 设备名称，非必须存在
    /// </summary>
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型，必须存在
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

    /// <summary>
    /// 变量信息列表，必须存在
    /// </summary>
    public List<ParameterConfigDto>? Parameters { get; set; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProtocolType ProtocolType { get; set; }
}
