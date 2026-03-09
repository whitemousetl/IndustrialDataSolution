namespace IndustrialDataProcessor.Domain.Entities;

public abstract class BaseEntity
{
    public DateTimeOffset CreateAt { get; set; } = DateTime.UtcNow;
}
