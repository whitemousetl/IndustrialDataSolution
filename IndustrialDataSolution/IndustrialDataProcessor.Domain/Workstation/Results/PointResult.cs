using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Domain.Workstation.Results;

public class PointResult//采集点读取结果
{
    public DataType? DataType { get; set; } // 数据类型
    public string Label { get; set; } = string.Empty; //标签，采集点名称
    public string Address { get; set; } = string.Empty;//地址
    public object? Value { get; set; } = null;//结果
    public bool ReadIsSuccess { get; set; } = false;//当次是否读取成功
    public string ErrorMsg { get; set; } = string.Empty; //错误信息
    public long ElapsedMs { get; set; } = 0;  //耗时，毫秒
    public Dictionary<string, object> Metadata { get; set; } = []; // 点额外信息
}