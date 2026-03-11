using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Exceptions;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;

/// <summary>
/// 协议配置抽象基类
/// 定义协议的基本属性和行为
/// </summary>
public abstract class ProtocolConfig
{
    private readonly List<EquipmentConfig> _equipments = [];

    /// <summary>
    /// 协议Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 接口类型，必须存在
    /// </summary>
    public abstract InterfaceType InterfaceType { get; }

    /// <summary>
    /// 协议类型，必须存在
    /// </summary>
    public ProtocolType ProtocolType { get; set; }

    /// <summary>
    /// 通讯延时,默认1000ms
    /// </summary>
    public int CommunicationDelay { get; set; } = 1000;

    /// <summary>
    /// 接收超时,默认500ms
    /// </summary>
    public int ReceiveTimeOut { get; set; } = 500;

    /// <summary>
    /// 连接超时，默认500ms
    /// </summary>
    public int ConnectTimeOut { get; set; } = 500;

    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 可选参数
    /// </summary>
    public string AdditionalOptions { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息列表（用于序列化）
    /// </summary>
    public List<EquipmentConfig> Equipments
    {
        get => _equipments;
        set
        {
            _equipments.Clear();
            if (value != null)
            {
                _equipments.AddRange(value);
            }
        }
    }

    /// <summary>
    /// 添加设备配置
    /// </summary>
    public void AddEquipment(EquipmentConfig equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        if (string.IsNullOrWhiteSpace(equipment.Id))
        {
            throw new DomainException("设备ID不能为空");
        }

        if (_equipments.Any(e => e.Id == equipment.Id))
        {
            throw new DomainException($"设备ID '{equipment.Id}' 已存在");
        }

        _equipments.Add(equipment);
    }

    /// <summary>
    /// 移除设备配置
    /// </summary>
    public bool RemoveEquipment(string equipmentId)
    {
        var equipment = _equipments.FirstOrDefault(e => e.Id == equipmentId);
        return equipment != null && _equipments.Remove(equipment);
    }

    /// <summary>
    /// 获取所有启用的设备
    /// </summary>
    public IEnumerable<EquipmentConfig> GetActiveEquipments()
    {
        return _equipments.Where(e => e.IsCollect);
    }
}