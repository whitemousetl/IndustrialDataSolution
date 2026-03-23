using IndustrialDataProcessor.Contracts.WorkstationDto;

namespace IndustrialDataProcessor.WpfClient.Interfaces;

public interface IWorkstationConfigService
{
    Task<WorkstationConfigDto?> GetConfigAsync();
    Task<bool> SaveConfigAsync(WorkstationConfigDto config);
    Task<bool> TestConnectionAsync();
}