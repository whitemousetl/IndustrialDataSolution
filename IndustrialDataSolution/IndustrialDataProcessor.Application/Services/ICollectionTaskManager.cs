namespace IndustrialDataProcessor.Application.Services;

public interface ICollectionTaskManager
{
    Task StartOrRestartAllTasksAsync(CancellationToken token);
}
