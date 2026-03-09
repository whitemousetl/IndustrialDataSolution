using MediatR;

namespace IndustrialDataProcessor.Application.Events;
/// <summary>
/// 工作站配置已更新事件
/// </summary>
public class WorkstationConfigUpdatedEvent : INotification
{
    public DateTime UpdatedTime { get; } = DateTime.Now;
}
