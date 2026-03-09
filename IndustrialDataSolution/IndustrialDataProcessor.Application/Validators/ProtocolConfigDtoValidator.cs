using FluentValidation;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Helpers;

namespace IndustrialDataProcessor.Application.Validators;

public class ProtocolConfigDtoValidator : AbstractValidator<ProtocolConfigDto>
{
    public ProtocolConfigDtoValidator()
    {
        // 通用必填字段
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("协议Id不能为空");

        RuleFor(x => x.ProtocolType)
            .IsInEnum()
            .WithMessage(x => $"协议Id: {x.Id} 协议类型无效");

        RuleFor(x => x.InterfaceType)
            .IsInEnum()
            .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 接口类型无效");

        RuleFor(x => x.Equipments)
            .NotEmpty()
            .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 设备列表不能为空");

        RuleForEach(x => x.Equipments)
            .Custom((equip, context) =>
            {
                // 从 ValidationContext 获取顶层的 Protocol
                var protocol = (ProtocolConfigDto)context.InstanceToValidate;

                // 【向下传递】将父级的 ProtocolType 赋值给当前的子级 Equipment
                equip.ProtocolType = protocol.ProtocolType;
            })
            .SetValidator(new EquipmentConfigDtoValidator());



        // 接口类型与协议类型的兼容性验证
        // 字段间关系，根据接口类型限制协议类型，比如接口类型是LAN时，如果协议类型是ModbusRtu，则不被支持
        RuleFor(x => x)
            .Must(x => ProtocolTypeHelper.IsProtocolTypeValidForInterface(x.InterfaceType, x.ProtocolType))
            .WithMessage(x => $"协议Id: {x.Id} 接口类型 {x.InterfaceType} 不支持协议类型 {x.ProtocolType}");

        // ==========================================
        // 可选字段的业务规则校验 (类型校验已经由反序列化器完成)
        // ==========================================

        RuleFor(x => x.CommunicationDelay)
            .GreaterThanOrEqualTo(0)
            .WithMessage(x => $"协议Id: {x.Id} 接口类型 {x.InterfaceType} 通讯延时不能小于0ms");

        RuleFor(x => x.ReceiveTimeOut)
            .GreaterThanOrEqualTo(500)
            .WithMessage(x => $"协议Id: {x.Id} 接口类型 {x.InterfaceType} 接收超时不能小于500ms");

        RuleFor(x => x.ConnectTimeOut)
            .GreaterThanOrEqualTo(500)
            .WithMessage(x => $"协议Id: {x.Id} 接口类型 {x.InterfaceType} 连接超时不能小于500ms");

        // ==========================================
        // 具体接口类型验证
        // ==========================================

        // 串口相关验证（InterfaceType.COM）
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

        // 网口相关验证（InterfaceType.LAN）
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

        // 数据库相关验证（InterfaceType.DATABASE）
        When(x => x.InterfaceType == InterfaceType.DATABASE, () =>
        {
            RuleFor(x => x.QuerySqlString)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 查询SQL语句不能为空");

            RuleFor(x => x)
            .Custom((protocol, context) =>
            {
                var hasConnectString = !string.IsNullOrWhiteSpace(protocol.DatabaseConnectString);
                var hasSeparateProperties = !string.IsNullOrWhiteSpace(protocol.IpAddress)
                                         || protocol.ProtocolPort.HasValue
                                         || !string.IsNullOrWhiteSpace(protocol.DatabaseName);

                if (!hasConnectString && !hasSeparateProperties)
                    context.AddFailure(nameof(protocol.DatabaseConnectString),
                        $"协议: {protocol.Id} 协议类型: {protocol.ProtocolType} 的数据库[连接字符串]或[IP地址/端口/数据库名]至少需要提供一种方式");
            });
        });

        // API 相关验证（InterfaceType.API）
        When(x => x.InterfaceType == InterfaceType.API, () =>
        {
            RuleFor(x => x.RequestMethod)
                .NotNull()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 请求方式不能为空")
                .IsInEnum()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 请求方式无效");

            RuleFor(x => x.AccessApiString)
                .NotEmpty()
                .WithMessage(x => $"协议Id: {x.Id} 协议类型: {x.ProtocolType} 访问API语句不能为空");
        });
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        var result = System.Net.IPAddress.TryParse(ipAddress, out var parsedIp)
               && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        return result;
    }
}
