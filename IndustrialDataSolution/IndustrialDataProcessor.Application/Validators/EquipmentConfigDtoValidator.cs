using FluentValidation;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;

namespace IndustrialDataProcessor.Application.Validators;

public class EquipmentConfigDtoValidator : AbstractValidator<EquipmentConfigDto>
{
	public EquipmentConfigDtoValidator()
	{
        // ============ 基础必填字段验证 ============
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备[Id]不能为空");

        // ============ 枚举值验证 (确保枚举值有效) ============
        RuleFor(x => x.EquipmentType)
            .IsInEnum()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.Id} 的[设备类型]值无效");

        // ============ 参数列表验证 ============
        RuleFor(x => x.Parameters)
            .NotNull()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.Id} 的[参数列表]不能为null")
            .NotEmpty()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.Id} 的[参数列表]不能为空");

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
