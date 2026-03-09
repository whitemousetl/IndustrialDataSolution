using FluentValidation;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Domain.Attributes;
using IndustrialDataProcessor.Domain.Enums;
using System.Reflection;

namespace IndustrialDataProcessor.Application.Validators;

public class ParameterConfigDtoValidator : AbstractValidator<ParameterConfigDto>
{
    public ParameterConfigDtoValidator()
    {
        // ============ 基础必填字段验证 ============
        RuleFor(x => x.Label).NotEmpty().WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 参数[标签]不能为空");
        RuleFor(x => x.Address).NotEmpty().WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 参数[地址]不能为空");

        // ============ 协议特定验证 (动态规则) ============
        RuleFor(x => x).Custom((parameter, context) =>
        {
            ValidateForProtocolType(parameter, context);
        });

        // ============ 枚举值验证 (确保枚举值有效) ============
        When(x => x.DataType.HasValue, () =>
        {
            RuleFor(x => x.DataType)
                .IsInEnum()
                .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[数据类型]值无效");
        });

        When(x => x.DataFormat.HasValue, () =>
        {
            RuleFor(x => x.DataFormat)
                .IsInEnum()
                .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[数据格式/字节序]值无效");
        });

        When(x => x.InstrumentType.HasValue, () =>
        {
            RuleFor(x => x.InstrumentType)
                .IsInEnum()
                .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[仪表类型]值无效");
        });

        // ============ 数值范围验证 ============
        RuleFor(x => x.Length)
            .GreaterThanOrEqualTo((ushort)0)
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[长度]不能为负数");

        RuleFor(x => x.Cycle)
            .GreaterThanOrEqualTo(0)
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[采集周期]不能为负数");

        // ============ MinValue <= MaxValue 逻辑验证 ============
        RuleFor(x => x)
            .Custom((parameter, context) =>
            {
                if (!string.IsNullOrWhiteSpace(parameter.MinValue)
                    && !string.IsNullOrWhiteSpace(parameter.MaxValue))
                {
                    if (decimal.TryParse(parameter.MinValue, out var min)
                        && decimal.TryParse(parameter.MaxValue, out var max))
                    {
                        if (min > max)
                        {
                            context.AddFailure(nameof(parameter.MaxValue),
                                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 的[最小值]({min})不能大于[最大值]({max})");
                        }
                    }
                }
            });
    }

    public void ValidateForProtocolType(ParameterConfigDto parameter, ValidationContext<ParameterConfigDto> context)
    {
        var fieldInfo = typeof(ProtocolType).GetField(parameter.ProtocolType.ToString());
        var attr = fieldInfo?.GetCustomAttribute<ProtocolValidateParameterAttribute>();

        if (attr == null) return;

        if (attr.RequireStationNo && string.IsNullOrWhiteSpace(parameter.StationNo))
            context.AddFailure(nameof(parameter.StationNo), $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[站号/通讯地址]");

        if (attr.RequireDataFormat && !parameter.DataFormat.HasValue)
            context.AddFailure(nameof(parameter.DataFormat), $" {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[数据格式/字节序]");

        if (attr.RequireDataType && !parameter.DataType.HasValue)
            context.AddFailure(nameof(parameter.DataType), $"协议类型 {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[数据类型]");

        if (attr.RequireAddressStartWithZero && parameter.AddressStartWithZero != true)
            context.AddFailure(nameof(parameter.AddressStartWithZero), $" {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[地址必须从0开始?]");

        if (attr.RequireInstrumentType && !parameter.InstrumentType.HasValue)
            context.AddFailure(nameof(parameter.InstrumentType), $" {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[仪表类型]");
    }
}
