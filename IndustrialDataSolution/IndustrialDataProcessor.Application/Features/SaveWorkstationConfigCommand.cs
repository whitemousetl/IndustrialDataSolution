using IndustrialDataProcessor.Application.Mappers;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Repositories;
using MediatR;
using System.Text.Json;

namespace IndustrialDataProcessor.Application.Features;

/// <summary>
/// 保存工作站配置命令
/// </summary>
/// <param name="Dto">工作站配置 DTO</param>
public record SaveWorkstationConfigCommand(WorkstationConfigDto Dto) : IRequest;

/// <summary>
/// 保存工作站配置命令处理器
/// </summary>
public class SaveWorkstationConfigCommandHandler(
    IWorkstationConfigPersistenceRepository repository, 
    IMediator mediator, 
    JsonSerializerOptions jsonOptions) : IRequestHandler<SaveWorkstationConfigCommand>
{
    private readonly IWorkstationConfigPersistenceRepository _repository = repository;
    private readonly IMediator _mediator = mediator;
    private readonly JsonSerializerOptions _options = jsonOptions;

    public async Task Handle(SaveWorkstationConfigCommand request, CancellationToken token)
    {
        // 1. 转换成领域层模型
        var domainEntity = request.Dto.ToDomain();
        var normalizedJson = JsonSerializer.Serialize(domainEntity, _options);

        // 2. 移交给基础设施，保存到数据库
        var entity = new WorkstationConfigEntity { JsonContent = normalizedJson };
        await _repository.AddAsync(entity, token);

        // 3. 发布配置更新事件 (触发后台异步操作)
        await _mediator.Publish(new WorkstationConfigUpdatedEvent(), token);
    }
}
