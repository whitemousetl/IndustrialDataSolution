using IndustrialDataProcessor.Domain.Entities;

namespace IndustrialDataProcessor.Domain.Repositories;

public interface IWorkstationConfigEntityRepository
{
    Task<WorkstationConfigEntity?> GetLatestAsync(CancellationToken token);
    Task AddAsync(WorkstationConfigEntity config, CancellationToken token);
}
