namespace IndustrialDataProcessor.Domain.Exceptions;

/// <summary>
/// 基础设施层异常
/// 用于：数据库宕机、第三方API超时、文件读写失败等
/// </summary>
public class InfrastructureException : IndustrialDataException
{
    public InfrastructureException(string message) : base(message) { }
    public InfrastructureException(string message, Exception inner) : base(message, inner) { }
}
