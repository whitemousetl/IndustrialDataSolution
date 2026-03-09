using IndustrialDataProcessor.Domain.Enums;
using System.IO.Ports;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Application.Dtos.WorkstationDto;

public class ProtocolConfigDto
{
    /// <summary>
    /// 协议Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 接口类型，必须存在
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InterfaceType InterfaceType { get; set; }

    /// <summary>
    /// 协议类型，必须存在
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProtocolType ProtocolType { get; set; }

    /// <summary>
    /// 通讯延时,默认500ms，有默认值，非必须存在
    /// </summary>
    public int CommunicationDelay { get; set; } = 500;

    /// <summary>
    /// 接收超时,默认500ms，有默认值，非必须存在
    /// </summary>
    public int ReceiveTimeOut { get; set; } = 500;

    /// <summary>
    /// 连接超时，默认500ms，有默认值，非必须存在
    /// </summary>
    public int ConnectTimeOut { get; set; } = 500;

    /// <summary>
    /// 账号，非必须存在
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密码，非必须存在
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 备注，非必须存在
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 可选参数，非必须存在
    /// </summary>
    public string AdditionalOptions { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息列表，必须存在
    /// </summary>
    public List<EquipmentConfigDto> Equipments { get; set; } = [];

    // 网络相关（LAN, DATABASE, API）
    public string? IpAddress { get; set; }
    public int? ProtocolPort { get; set; }
    public string? Gateway { get; set; }

    // 串口相关（COM）
    public string? SerialPortName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BaudRateType? BaudRate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataBitsType? DataBits { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DomainParity? Parity { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DomainStopBits? StopBits { get; set; }

    // 数据库相关（DATABASE）
    public string? DatabaseName { get; set; }
    public string? DatabaseConnectString { get; set; }
    public string? QuerySqlString { get; set; }

    // API 相关（API）
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RequestMethod? RequestMethod { get; set; }
    public string? AccessApiString { get; set; }
}
