using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Serialization.Converters;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.Repositories;

public class WorkstationConfigRepository : IWorkstationConfigRepository
{
    private readonly IWorkstationConfigEntityRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkstationConfigRepository(IWorkstationConfigEntityRepository repository)
    {
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // 忽略大小写
            Converters = { new ProtocolConfigJsonConverter() } // 注册多态转换器
        };
    }

    public async Task<WorkstationConfig?> GetLatestParsedConfigAsync(CancellationToken token)
    {
        // 1. 从数据库获取最新的实体 (假设你的实体叫 WorkstationConfigPo)
        var entity = await _repository.GetLatestAsync(token);

        if (entity == null || string.IsNullOrWhiteSpace(entity.JsonContent))
            return null;

        try
        {
            // 2. 在基础设施层完成 JSON 到 领域模型 的转换
            var config = JsonSerializer.Deserialize<WorkstationConfig>(entity.JsonContent, _jsonOptions);
            return config;
        }
        catch (JsonException ex)
        {
            // 记录日志，配置格式错误
            throw new Exception("工作站配置 JSON 解析失败", ex);
        }
    }
}