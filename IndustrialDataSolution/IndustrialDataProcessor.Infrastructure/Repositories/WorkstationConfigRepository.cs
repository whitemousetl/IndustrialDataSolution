using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Serialization.Converters;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.Repositories;

public class WorkstationConfigRepository : IWorkstationConfigRepository
{
    private readonly IWorkstationConfigEntityRepository _repository;
    private readonly ILogger<WorkstationConfigRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkstationConfigRepository(
        IWorkstationConfigEntityRepository repository,
        ILogger<WorkstationConfigRepository> logger)
    {
        _repository = repository;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // 忽略大小写
            Converters = { new ProtocolConfigJsonConverter() } // 注册多态转换器
        };
    }

    public async Task<WorkstationConfig?> GetLatestParsedConfigAsync(CancellationToken token)
    {
        // 1. 从数据库获取最新的实体
        var entity = await _repository.GetLatestAsync(token);

        if (entity == null)
        {
            _logger.LogWarning("数据库中没有找到工作站配置实体");
            return null;
        }

        if (string.IsNullOrWhiteSpace(entity.JsonContent))
        {
            _logger.LogWarning("工作站配置实体的JsonContent为空");
            return null;
        }

        _logger.LogDebug("开始反序列化工作站配置, JsonContent长度: {Length}, 前100字符: {Preview}", 
            entity.JsonContent.Length, 
            entity.JsonContent.Length > 100 ? entity.JsonContent[..100] : entity.JsonContent);

        try
        {
            // 2. 在基础设施层完成 JSON 到 领域模型 的转换
            var config = JsonSerializer.Deserialize<WorkstationConfig>(entity.JsonContent, _jsonOptions);
            
            if (config != null)
            {
                _logger.LogDebug("反序列化成功 - Id: {Id}, IpAddress: {IpAddress}, Protocols数量: {Count}", 
                    config.Id, config.IpAddress, config.Protocols?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("反序列化结果为null");
            }
            
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "工作站配置 JSON 解析失败, JsonContent: {Content}", entity.JsonContent);
            throw new Exception("工作站配置 JSON 解析失败", ex);
        }
    }
}