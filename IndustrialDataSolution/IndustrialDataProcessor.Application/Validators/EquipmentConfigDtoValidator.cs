using FluentValidation;
using IndustrialDataProcessor.Contracts.WorkstationDto;

namespace IndustrialDataProcessor.Application.Validators;

/// <summary>
/// 设备配置验证器
/// </summary>
public class EquipmentConfigDtoValidator : AbstractValidator<EquipmentConfigDto>
{
    public EquipmentConfigDtoValidator()
    {
        // ============ Id 不可为空 ============
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备[Id]不能为空");

        // ============ IsCollect 可为空，默认值 true（已在 DTO 中设置） ============
        // 不需要验证

        // ============ Name 可为空 ============
        // 不需要验证

        // ============ EquipmentType 不可为空且枚举合法 ============
        RuleFor(x => x.EquipmentType)
            .IsInEnum()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.Id} 的[设备类型]值无效");

        // ============ Parameters 可为空，为空则默认为空集合（已在 DTO 中设置） ============
        // 不需要验证 NotNull 或 NotEmpty

        // ============ 参数列表子项验证及数据下传 ============
        RuleForEach(x => x.Parameters)
            .Custom((param, context) =>
            {
                // 获取上层的 Equipment
                var equipment = (EquipmentConfigDto)context.InstanceToValidate;

                // 【向下传递】将 Equipment 的属性交给当前的 Parameter
                param.ProtocolType = equipment.ProtocolType;
                param.EquipmentId = equipment.Id;
            })
            // 赋值后，再走参数的验证
            .SetValidator(new ParameterConfigDtoValidator()!)
            .When(x => x.Parameters != null && x.Parameters.Count > 0);
    }
}
