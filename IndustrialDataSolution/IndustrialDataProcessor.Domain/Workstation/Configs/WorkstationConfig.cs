using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;

/// <summary>
/// 工作站聚合根
/// 管理工作站的整体配置，包括协议、设备和参数
/// </summary>
public class WorkstationConfig : IAggregateRoot
{
    private List<ProtocolConfig> _protocols = [];

    /// <summary>
    /// 工作站Id，必须存在
    /// </summary>
    [JsonInclude]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// 工作站名称
    /// </summary>
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// IP地址，必须存在
    /// </summary>
    [JsonInclude]
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>
    /// 协议信息列表
    /// </summary>
    [JsonInclude]
    public List<ProtocolConfig> Protocols
    {
        get => _protocols;
        private set
        {
            _protocols = value ?? [];
        }
    }

    /// <summary>
    /// 无参构造函数（用于反序列化）
    /// </summary>
    public WorkstationConfig() { }

    /// <summary>
    /// 创建工作站聚合根
    /// </summary>
    public WorkstationConfig(string id, string name, string ipAddress)
    {
        ValidateId(id);
        ValidateIpAddress(ipAddress);

        Id = id;
        Name = name ?? string.Empty;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// 添加协议配置
    /// </summary>
    public void AddProtocol(ProtocolConfig protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        if (string.IsNullOrWhiteSpace(protocol.Id))
        {
            throw new DomainException("协议ID不能为空");
        }

        if (_protocols.Any(p => p.Id == protocol.Id))
        {
            throw new DomainException($"协议ID '{protocol.Id}' 已存在");
        }

        _protocols.Add(protocol);
    }

    /// <summary>
    /// 移除协议配置
    /// </summary>
    public bool RemoveProtocol(string protocolId)
    {
        var protocol = _protocols.FirstOrDefault(p => p.Id == protocolId);
        return protocol != null && _protocols.Remove(protocol);
    }

    /// <summary>
    /// 清空所有协议配置
    /// </summary>
    public void ClearProtocols()
    {
        _protocols.Clear();
    }

    /// <summary>
    /// 更新工作站基本信息
    /// </summary>
    public void UpdateBasicInfo(string name, string ipAddress)
    {
        ValidateIpAddress(ipAddress);
        Name = name ?? string.Empty;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// 获取所有启用的协议
    /// </summary>
    public IEnumerable<ProtocolConfig> GetActiveProtocols()
    {
        return _protocols.Where(p => p.Equipments.Any(e => e.IsCollect));
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("工作站ID不能为空");
        }
    }

    private static void ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new DomainException("IP地址不能为空");
        }
    }
}