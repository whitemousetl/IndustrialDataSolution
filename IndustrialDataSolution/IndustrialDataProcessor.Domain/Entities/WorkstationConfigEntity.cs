namespace IndustrialDataProcessor.Domain.Entities;

/// <summary>
/// 工作站配置持久化实体
/// 用于存储 JSON 格式的配置数据
/// </summary>
public class WorkstationConfigEntity : BaseEntity
{
    /// <summary>
    /// JSON 格式的配置内容
    /// </summary>
    public string JsonContent { get; set; } = string.Empty;
}
