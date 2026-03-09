namespace IndustrialDataProcessor.Domain.Exceptions;

// 1. 全局基类异常 (放在 Domain 层或 Shared Kernel 层)
public abstract class IndustrialDataException : Exception
{
    protected IndustrialDataException(string message) : base(message) { }
    protected IndustrialDataException(string message, Exception inner) : base(message, inner) { }
}
