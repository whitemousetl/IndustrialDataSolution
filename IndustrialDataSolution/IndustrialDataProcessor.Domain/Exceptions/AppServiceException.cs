namespace IndustrialDataProcessor.Domain.Exceptions;

/// <summary>
/// 应用层异常
/// 用于：找不到实体、并发冲突、工作流执行失败等
/// </summary>
public class AppServiceException : IndustrialDataException
{
    public AppServiceException(string message) : base(message) { }
}
