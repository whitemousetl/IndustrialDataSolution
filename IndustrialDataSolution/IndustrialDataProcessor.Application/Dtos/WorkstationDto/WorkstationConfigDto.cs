namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;

/// <summary>
/// 工作站配置数据传输对象
/// 用于 API 层与客户端之间的数据传输
/// </summary>
public class WorkstationConfigDto
{
    /// <summary>
    /// 工作站Id
    /// <para>可选字段，允许为空或空字符串</para>
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 工作站名称
    /// <para>可选字段，允许为空</para>
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 工作站IP地址
    /// <para>可选字段，允许为空或空字符串</para>
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 协议信息列表
    /// <para>必填字段，不能为null且至少包含一个协议配置</para>
    /// </summary>
    public List<ProtocolConfigDto> Protocols { get; set; } = [];
}
