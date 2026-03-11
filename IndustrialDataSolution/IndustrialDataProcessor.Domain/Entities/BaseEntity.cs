namespace IndustrialDataProcessor.Domain.Entities;

/// <summary>
/// 实体基类
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 聚合根接口标识
/// </summary>
public interface IAggregateRoot;

/// <summary>
/// 聚合根基类
/// </summary>
/// <typeparam name="TId">标识类型</typeparam>
public abstract class AggregateRoot<TId> : BaseEntity, IAggregateRoot
{
    /// <summary>
    /// 聚合根标识
    /// </summary>
    public TId Id { get; protected set; } = default!;
}
