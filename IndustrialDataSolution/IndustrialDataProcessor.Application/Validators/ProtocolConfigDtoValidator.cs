using FluentValidation;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Helpers;

namespace IndustrialDataProcessor.Application.Validators;

/// <summary>
/// 协议配置验证器
/// </summary>
public class ProtocolConfigDtoValidator : AbstractValidator<ProtocolConfigDto>
{
    public ProtocolConfigDtoValidator()
    {
        // ==========================================
        // 基础必填字段验证
        // ==========================================

        // Id 不能为空
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("协议Id不能为空");

        // 接口类型必须是合法枚举
        RuleFor(x => x.InterfaceType)
            .IsInEnum()
            .WithMessage(x => $"协议Id: {x.Id} 接口类型无效，必须是 {string.Join(", ", Enum.GetNames<InterfaceType>())} 之一");

        // 协议类型必须是合法枚举
        RuleFor(x => x.ProtocolType)
            .IsInEnum()
            .WithMessage(x => $"协议Id: {x.Id} 协议类型无效");

        // 接口类型与协议类型的兼容性验证
        RuleFor(x => x)
            .Must(x => ProtocolTypeHelper.IsProtocolTypeValidForInterface(x.InterfaceType, x.ProtocolType))
            .WithMessage(x => $"协议Id: {x.Id} 接口类型 {x.InterfaceType} 不支持协议类型 {x.ProtocolType}");

        // 设备列表不可为空且集合元素数量不能为零
        RuleFor(x => x.Equipments)
            .NotNull()
            .WithMessage(x => $"协议Id: {x.Id} 设备列表不能为null")
            .NotEmpty()
            .WithMessage(x => $"协议Id: {x.Id} 设备列表不能为空，至少需要一个设备配置");

        // 设备列表子项验证及数据下传
        RuleForEach(x => x.Equipments)
            .Custom((equip, context) =>
            {
                var protocol = (ProtocolConfigDto)context.InstanceToValidate;
                // 向下传递 ProtocolType
                equip.ProtocolType = protocol.ProtocolType;
            })
            .SetValidator(new EquipmentConfigDtoValidator())
            .When(x => x.Equipments != null && x.Equipments.Count > 0);

        // ==========================================
        // 可选字段的默认值处理和业务规则校验
        // CommunicationDelay: 默认50000ms，空或P0则默认50000ms
        // ReceiveTimeOut: 默认10000ms
        // ConnectTimeOut: 默认10000ms
        // Account, Password, Remark, AdditionalOptions, Gateway: 可为空
        // ==========================================

        // 这些字段不需要验证，只需要在使用时应用默认值逻辑

        // ==========================================
        // 接口类型 LAN 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.LAN, () =>
        {
            RuleFor(x => x.IpAddress)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} IP地址不能为空")
                .Must(BeValidIpAddress)
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} IP地址格式不正确");

            RuleFor(x => x.ProtocolPort)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 端口不能为空")
                .GreaterThan(0)
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 端口必须大于0")
                .LessThanOrEqualTo(65535)
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 端口不能超过65535");
        });

        // ==========================================
        // 接口类型 COM 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.COM, () =>
        {
            RuleFor(x => x.SerialPortName)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 串口名称不能为空");

            RuleFor(x => x.BaudRate)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 波特率不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 波特率无效");

            RuleFor(x => x.DataBits)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 数据位不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 数据位无效");

            RuleFor(x => x.Parity)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 校验位不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 校验位无效");

            RuleFor(x => x.StopBits)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 停止位不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 停止位无效");
        });

        // ==========================================
        // 接口类型 DATABASE 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.DATABASE, () =>
        {
            RuleFor(x => x.DatabaseName)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 数据库名称不能为空");

            RuleFor(x => x.DatabaseConnectString)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 数据库连接字符串不能为空");

            RuleFor(x => x.QuerySqlString)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 查询SQL语句不能为空");
        });

        // ==========================================
        // 接口类型 API 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.API, () =>
        {
            RuleFor(x => x.RequestMethod)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 请求方式不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 请求方式无效");

            RuleFor(x => x.AccessApiString)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 访问API地址不能为空");
        });
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out var parsedIp)
               && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }
}
