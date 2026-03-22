using FluentValidation;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Attributes;
using IndustrialDataProcessor.Domain.Enums;
using System.Reflection;

namespace IndustrialDataProcessor.Application.Validators;

/// <summary>
/// 参数配置验证器
/// </summary>
public class ParameterConfigDtoValidator : AbstractValidator<ParameterConfigDto>
{
    // Length 默认值
    private const ushort DefaultStringLength = 10;

    public ParameterConfigDtoValidator()
    {
        // ============ Label 不可为空 ============
        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 参数[标签]不能为空");

        // ============ Address 强制所有协议都要求不为空 ============
        // 即使是 API 协议，也需要有一个标识（可以是 JSON 路径等）
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 参数[地址]不能为空");

        // ============ IsMonitor 可为空，空默认 true ============
        // 这个逻辑在业务层处理默认值，验证器不需要验证

        // ============ 协议特性驱动的动态验证 ============
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

        // ============ Length 验证：当 DataType 是 String 时必填，默认 10 ============
        RuleFor(x => x.Length)
            .NotNull()
            .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 当数据类型为String时[长度]不能为空")
            .When(x => x.DataType == DataType.String);

        // ============ 数值范围验证 ============
        When(x => x.Length.HasValue, () =>
        {
            RuleFor(x => x.Length)
                .GreaterThanOrEqualTo((ushort)0)
                .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[长度]不能为负数");
        });

        When(x => x.Cycle.HasValue, () =>
        {
            RuleFor(x => x.Cycle)
                .GreaterThanOrEqualTo(0)
                .WithMessage(x => $"协议类型: {x.ProtocolType} 设备: {x.EquipmentId} 标签: {x.Label} 的[采集周期]不能为负数");
        });

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

        // ============ DefaultValue, Cycle, PositiveExpression, MinValue, MaxValue, Value 都可为空 ============
        // 不需要验证
    }

    /// <summary>
    /// 根据协议类型的特性进行动态验证
    /// </summary>
    private static void ValidateForProtocolType(ParameterConfigDto parameter, ValidationContext<ParameterConfigDto> context)
    {
        var fieldInfo = typeof(ProtocolType).GetField(parameter.ProtocolType.ToString());
        var attr = fieldInfo?.GetCustomAttribute<ProtocolValidateParameterAttribute>();

        if (attr == null) return;

        // StationNo 根据 requireStationNo 特性判断
        if (attr.RequireStationNo && string.IsNullOrWhiteSpace(parameter.StationNo))
            context.AddFailure(nameof(parameter.StationNo), 
                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[站号/通讯地址]");

        // DataFormat 根据 requireDataFormat 特性判断
        if (attr.RequireDataFormat && !parameter.DataFormat.HasValue)
            context.AddFailure(nameof(parameter.DataFormat), 
                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[数据格式/字节序]");

        // DataType 根据 requireDataType 特性判断
        if (attr.RequireDataType && !parameter.DataType.HasValue)
            context.AddFailure(nameof(parameter.DataType), 
                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[数据类型]");

        // AddressStartWithZero 根据 requireAddressStartWithZero 特性判断
        if (attr.RequireAddressStartWithZero && !parameter.AddressStartWithZero.HasValue)
            context.AddFailure(nameof(parameter.AddressStartWithZero), 
                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[地址从0开始?]");

        // InstrumentType 根据 requireInstrumentType 特性判断
        if (attr.RequireInstrumentType && !parameter.InstrumentType.HasValue)
            context.AddFailure(nameof(parameter.InstrumentType), 
                $"协议类型: {parameter.ProtocolType} 设备: {parameter.EquipmentId} 标签: {parameter.Label} 要求参数必须包含[仪表类型]");
    }
}
