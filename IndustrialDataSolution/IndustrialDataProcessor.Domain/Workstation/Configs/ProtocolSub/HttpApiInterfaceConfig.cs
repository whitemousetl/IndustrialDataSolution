using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
/// <summary>
/// Api协议信息
/// </summary>
public class HttpApiInterfaceConfig : ProtocolConfig
{
    /// <summary>,必须存在
    /// 接口类型
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.API;

    /// <summary>
    /// 请求方式（默认Get）,必须存在
    /// </summary>
    public RequestMethod? RequestMethod { get; set; }

    /// <summary>
    /// 访问API语句,必须存在
    /// </summary>
    public string AccessApiString { get; set; } = string.Empty;

    /// <summary>
    /// 代理网关
    /// </summary>
    public string Gateway { get; set; } = string.Empty;
}

