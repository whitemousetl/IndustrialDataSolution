using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Configs;

/// <summary>
/// 参数配置值对象
/// 表示设备下的一个采集参数
/// </summary>
public class ParameterConfig
{
    /// <summary>
    /// 参数名，必须存在
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 地址, 虚拟点固定地址VirtualPoint，必须存在
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 是否监控，默认false
    /// </summary>
    public bool IsMonitor { get; set; }

    /// <summary>
    /// 站号
    /// </summary>
    public string? StationNo { get; set; }

    /// <summary>
    /// 协议格式，解析或生成格式，大端序小端序
    /// </summary>
    public DomainDataFormat? DataFormat { get; set; }

    /// <summary>
    /// 偏移量，地址从0开始？
    /// </summary>
    public bool? AddressStartWithZero { get; set; }

    /// <summary>
    /// 仪表类型，CJT188专用
    /// </summary>
    public InstrumentType? InstrumentType { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public DataType? DataType { get; set; }

    /// <summary>
    /// 长度
    /// </summary>
    public ushort Length { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 采集周期
    /// </summary>
    public int Cycle { get; set; }

    /// <summary>
    /// 正表达式，一元一次方程，进制转换，虚拟点计算
    /// </summary>
    public string? PositiveExpression { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// 写入才有值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 无参构造函数（用于反序列化）
    /// </summary>
    public ParameterConfig() { }

    /// <summary>
    /// 创建参数配置
    /// </summary>
    public ParameterConfig(string label, string address)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("参数标签不能为空", nameof(label));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("参数地址不能为空", nameof(address));

        Label = label;
        Address = address;
    }

    /// <summary>
    /// 是否为虚拟点
    /// </summary>
    public bool IsVirtualPoint => !string.IsNullOrEmpty(Address) && 
        Address.Contains("VirtualPoint", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否有表达式转换
    /// </summary>
    public bool HasExpression => !string.IsNullOrWhiteSpace(PositiveExpression);

    /// <summary>
    /// 是否有范围限制
    /// </summary>
    public bool HasRangeLimit => !string.IsNullOrWhiteSpace(MinValue) || !string.IsNullOrWhiteSpace(MaxValue);
}
