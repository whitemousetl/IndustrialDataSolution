namespace IndustrialDataProcessor.Domain.Exceptions;

// 3. 应用层异常 (放在 Application 层)
// 用于：找不到实体、并发冲突、工作流执行失败等
public class AppServiceException : IndustrialDataException
{
    public AppServiceException(string message) : base(message) { }
}
