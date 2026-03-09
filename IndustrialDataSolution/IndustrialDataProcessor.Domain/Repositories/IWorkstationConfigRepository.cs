using IndustrialDataProcessor.Domain.Workstation.Configs;

namespace IndustrialDataProcessor.Domain.Repositories;

public interface IWorkstationConfigRepository
{
    /// <summary>
    /// 获取并解析最新的工作站配置
    /// </summary>
    Task<WorkstationConfig?> GetLatestParsedConfigAsync(CancellationToken token);
}
