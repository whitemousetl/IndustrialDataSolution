using FluentValidation;
using IndustrialDataProcessor.Application.Features;

namespace IndustrialDataProcessor.Application.Validators;

/// <summary>
/// 保存工作站配置命令验证器
/// </summary>
public class SaveWorkstationConfigCommandValidator : AbstractValidator<SaveWorkstationConfigCommand>
{
    public SaveWorkstationConfigCommandValidator()
    {
        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage("工作站配置数据不能为null")
            .SetValidator(new WorkstationConfigDtoValidator()!)
            .When(x => x.Dto != null);
    }
}
