namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 串口校验位（领域层抽象，与具体实现无关）
/// </summary>
public enum DomainParity
{
    None,
    Odd,
    Even,
    Mark,
    Space
}