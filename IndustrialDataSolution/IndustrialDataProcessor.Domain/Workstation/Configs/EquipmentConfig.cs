using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Exceptions;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;

/// <summary>
/// 设备配置实体
/// 表示协议下的一个设备
/// </summary>
public class EquipmentConfig
{
    private List<ParameterConfig>? _parameters;

    /// <summary>
    /// 设备Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 该设备是否采集，必须存在
    /// </summary>
    public bool IsCollect { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 设备类型，必须存在
    /// </summary>
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

    /// <summary>
    /// 变量信息列表
    /// </summary>
    public List<ParameterConfig>? Parameters
    {
        get => _parameters;
        set
        {
            _parameters = value;
        }
    }

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public EquipmentConfig() { }

    /// <summary>
    /// 创建设备配置
    /// </summary>
    public EquipmentConfig(string id, bool isCollect, string? name = null, EquipmentType equipmentType = EquipmentType.Equipment)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("设备ID不能为空");
        }

        Id = id;
        IsCollect = isCollect;
        Name = name;
        EquipmentType = equipmentType;
    }

    /// <summary>
    /// 添加参数配置
    /// </summary>
    public void AddParameter(ParameterConfig parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        _parameters ??= [];

        if (string.IsNullOrWhiteSpace(parameter.Label))
        {
            throw new DomainException("参数标签不能为空");
        }

        if (_parameters.Any(p => p.Label == parameter.Label))
        {
            throw new DomainException($"参数标签 '{parameter.Label}' 已存在");
        }

        _parameters.Add(parameter);
    }

    /// <summary>
    /// 移除参数配置
    /// </summary>
    public bool RemoveParameter(string label)
    {
        if (_parameters == null) return false;
        var parameter = _parameters.FirstOrDefault(p => p.Label == label);
        return parameter != null && _parameters.Remove(parameter);
    }

    /// <summary>
    /// 获取所有监控参数
    /// </summary>
    public IEnumerable<ParameterConfig> GetMonitorParameters()
    {
        return _parameters?.Where(p => p.IsMonitor) ?? [];
    }
}
