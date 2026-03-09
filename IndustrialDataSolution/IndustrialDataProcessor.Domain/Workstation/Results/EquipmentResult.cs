namespace IndustrialDataProcessor.Domain.Workstation.Results;

public class EquipmentResult // 设备读取结果
{
    public string EquipmentId { get; set; } = string.Empty; // 设备ID
    public string EquipmentName { get; set; } = string.Empty; // 设备名称
    public List<PointResult> PointResults { get; set; } = []; // 采集点结果
    public bool ReadIsSuccess { get; set; } = false;//当次是否读取成功
    public string ErrorMsg { get; set; } = string.Empty; //错误信息
    public long ElapsedMs { get; set; } = 0;  //耗时，毫秒
    public int TotalPoints { get; set; } = 0;//总点数
    public int SuccessPoints { get; set; } = 0;//成功点数
    public int FailedPoints { get; set; } = 0;//失败点数
    public string StartTime { get; set; } = string.Empty;//开始时间
    public string EndTime { get; set; } = string.Empty;//结束时间
    public Dictionary<string, object> Metadata { get; set; } = []; // 设备额外信息
}
