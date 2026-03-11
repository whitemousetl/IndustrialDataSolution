namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;

/// <summary>
/// 工作站配置数据传输对象
/// 用于 API 层与客户端之间的数据传输
/// </summary>
public class WorkstationConfigDto
{
    /// <summary>
    /// 工作站Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 工作站名称
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
