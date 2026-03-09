using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
/// <summary>
/// 数据库协议信息
/// </summary>
public class DatabaseInterfaceConfig : ProtocolConfig
{
    /// <summary>
    /// 接口类型,必须存在
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.DATABASE;

    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int ProtocolPort { get; set; }

    /// <summary>
    /// 数据库名
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string DatabaseConnectString { get; set; } = string.Empty;

    /// <summary>
    /// 查询SQL语句,必须存在
    /// </summary>
    public string QuerySqlString { get; set; } = string.Empty;

    /// <summary>
    /// 代理网关
    /// </summary>
    public string Gateway { get; set; } = string.Empty;
}

