namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;
/// <summary>
/// 边缘信息
/// </summary>
public class WorkstationConfigDto
{
    /// <summary>
    /// 边缘Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 边缘名称
    /// </summary>
    public string? Name { get; set; } 

    /// <summary>
    /// IP地址，必须存在
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 协议信息列表，必须存在
    /// </summary>
    public List<ProtocolConfigDto> Protocols { get; set; } = [];
}
