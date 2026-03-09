namespace IndustrialDataProcessor.Domain.Exceptions;
// 2. 领域层异常 (放在 Domain 层)
// 用于：破坏了业务规则、聚合根内部状态无效等
public class DomainException : IndustrialDataException
{
    public DomainException(string message) : base(message) { }
}