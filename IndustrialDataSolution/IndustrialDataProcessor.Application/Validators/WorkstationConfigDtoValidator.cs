using FluentValidation;
using IndustrialDataProcessor.Contracts.WorkstationDto;

namespace IndustrialDataProcessor.Application.Validators;

/// <summary>
/// 工作站配置验证器
/// Id, IpAddress, Name 都可以为空
/// Protocols 不能为空且元素数量不能为零
/// </summary>
public class WorkstationConfigDtoValidator : AbstractValidator<WorkstationConfigDto>
{
    public WorkstationConfigDtoValidator()
    {
        // Id, IpAddress, Name 可为空，不做验证

        // 协议列表不能为空且元素数量不能为零
        RuleFor(x => x.Protocols)
            .NotNull()
            .WithMessage("协议列表不能为null")
            .NotEmpty()
            .WithMessage("协议列表不能为空，至少需要一个协议配置")
            .ForEach(protocol => protocol.SetValidator(new ProtocolConfigDtoValidator()));
    }
}
