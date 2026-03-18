namespace IndustrialDataProcessor.Infrastructure.OpcUa;

public class OpcUaOptions
{
    public const string SectionName = "OpcUa";

    /// <summary>
    /// OPC UA 服务器监听端点地址
    /// </summary>
    public string Endpoint { get; set; } = "opc.tcp://0.0.0.0:14840/WorkstationServer";

}
