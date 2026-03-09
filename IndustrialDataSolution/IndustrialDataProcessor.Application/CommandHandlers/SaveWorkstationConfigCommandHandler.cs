using IndustrialDataProcessor.Application.Commands;
using IndustrialDataProcessor.Application.Events;
using IndustrialDataProcessor.Application.Mappers;
using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Repositories;
using MediatR;
using System.Text.Json;

namespace IndustrialDataProcessor.Application.CommandHandlers;

public class SaveWorkstationConfigCommandHandler(IWorkstationConfigEntityRepository repository, IMediator mediator, JsonSerializerOptions jsonOptions)
    : IRequestHandler<SaveWorkstationConfigCommand>
{
    private readonly IWorkstationConfigEntityRepository _repository = repository;
    private readonly IMediator _mediator = mediator;
    private readonly JsonSerializerOptions _options = jsonOptions;

    public async Task Handle(SaveWorkstationConfigCommand request, CancellationToken token)
    {
        // 1. 转换成领域层模型
        var domainEntity = request.dto.ToDomain();
        var normalizedJson = JsonSerializer.Serialize(domainEntity, _options);

        // 2. 移交给基础设施，保存到数据库
        var entity = new WorkstationConfigEntity { JsonContent = normalizedJson };
        await _repository.AddAsync(entity, token);

        // 3. 发布配置更新事件 (触发清除缓存等操作)
        await _mediator.Publish(new WorkstationConfigUpdatedEvent(), token);
    }
}
