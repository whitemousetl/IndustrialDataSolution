using FluentValidation;
using IndustrialDataProcessor.Contracts.WorkstationDto;
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
            .WithMessage(x => $"接口类型无效，必须是 {string.Join(", ", Enum.GetNames<InterfaceType>())} 之一");

        // 协议类型必须是合法枚举
        RuleFor(x => x.ProtocolType)
            .IsInEnum()
            .WithMessage(x => $"协议类型无效，必须是 {string.Join(", ", Enum.GetNames<ProtocolType>())} 之一");

        // 接口类型与协议类型的兼容性验证
        RuleFor(x => x)
            .Must(x => ProtocolTypeHelper.IsProtocolTypeValidForInterface(x.InterfaceType, x.ProtocolType))
            .WithMessage(x => $"接口类型 {x.InterfaceType} 不支持协议类型 {x.ProtocolType}");

        // 设备列表不可为空且集合元素数量不能为零
        RuleFor(x => x.Equipments)
            .NotNull()
            .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型 {x.ProtocolType} 设备列表不能为null")
            .NotEmpty()
            .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型 {x.ProtocolType} 设备列表不能为空，至少需要一个设备配置");

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
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} IP地址不能为空")
                .Must(BeValidIpAddress)
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} IP地址格式不正确");

            RuleFor(x => x.ProtocolPort)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口不能为空")
                .GreaterThan(0)
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口必须大于0")
                .LessThanOrEqualTo(65535)
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口不能超过65535");
        });

        // ==========================================
        // 接口类型 COM 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.COM, () =>
        {
            RuleFor(x => x.SerialPortName)
                .NotEmpty()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 串口名称不能为空");

            RuleFor(x => x.BaudRate)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 波特率不能为空")
                .IsInEnum()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 波特率无效");

            RuleFor(x => x.DataBits)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 数据位不能为空")
                .IsInEnum()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 数据位无效");

            RuleFor(x => x.Parity)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 校验位不能为空")
                .IsInEnum()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 校验位无效");

            RuleFor(x => x.StopBits)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 停止位不能为空")
                .IsInEnum()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 停止位无效");
        });

        // ==========================================
        // 接口类型 DATABASE 验证
        // 策略：连接字符串 与 分字段（IP/端口/库名/账号/密码）二选一
        //   ● QuerySqlString 始终必填
        //   ● 有 DatabaseConnectString → 验证可解析性 + 包含必要键
        //   ● 无 DatabaseConnectString → 强制要求 IP、端口、数据库名、账号、密码
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.DATABASE, () =>
        {
            // 1. 查询语句始终必填
            RuleFor(x => x.QuerySqlString)
                .NotEmpty()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 查询SQL语句不能为空");

            // 2. 必须至少提供连接字符串或分字段之一（二选一的顶层守卫）
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DatabaseConnectString)
                    || (!string.IsNullOrWhiteSpace(x.IpAddress)
                        && x.ProtocolPort.HasValue
                        && !string.IsNullOrWhiteSpace(x.DatabaseName)
                        && !string.IsNullOrWhiteSpace(x.Account)
                        && !string.IsNullOrWhiteSpace(x.Password)))
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 必须提供完整的连接字符串，或同时提供 IP、端口、数据库名、账号和密码");

            // 3. 有连接字符串 → 验证格式和必要键
            When(x => !string.IsNullOrWhiteSpace(x.DatabaseConnectString), () =>
            {
                RuleFor(x => x.DatabaseConnectString)
                    .Must(BeAParsableConnectionString)
                    .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 连接字符串无法解析")
                    .Must(ConnectionStringContainsRequiredKeys)
                    .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 连接字符串缺少必要项（需含 Host/Server、Database、User+Password 或 Integrated Security）");
            });

            // 4. 无连接字符串 → 强制要求分字段，并给出每个字段的精确错误
            When(x => string.IsNullOrWhiteSpace(x.DatabaseConnectString), () =>
            {
                RuleFor(x => x.IpAddress)
                    .NotEmpty()
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} IP地址不能为空")
                    .Must(BeValidIpAddress)
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} IP地址格式不正确");

                RuleFor(x => x.ProtocolPort)
                    .NotNull()
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口不能为空")
                    .GreaterThan(0)
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口必须大于0")
                    .LessThanOrEqualTo(65535)
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 端口不能超过65535");

                RuleFor(x => x.DatabaseName)
                    .NotEmpty()
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 数据库名称不能为空");

                RuleFor(x => x.Account)
                    .NotEmpty()
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 账号不能为空");

                RuleFor(x => x.Password)
                    .NotEmpty()
                    .WithMessage(x => $"当未提供连接字符串时，接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 密码不能为空");
            });
        });

        // ==========================================
        // 接口类型 API 验证
        // ==========================================
        When(x => x.InterfaceType == InterfaceType.API, () =>
        {
            RuleFor(x => x.RequestMethod)
                .NotNull()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 请求方式不能为空")
                .IsInEnum()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 请求方式无效");

            RuleFor(x => x.AccessApiString)
                .NotEmpty()
                .WithMessage(x => $"接口类型 {x.InterfaceType} 协议类型: {x.ProtocolType} 访问API地址不能为空");
        });
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out var parsedIp)
               && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    private static bool BeAParsableConnectionString(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return false;
        try
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder();
            builder.ConnectionString = cs;
            return builder.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查连接字符串是否包含构建数据库连接的必要键：
    /// <list type="bullet">
    ///   <item>主机/服务器（Host / Server / Data Source）</item>
    ///   <item>数据库名（Database / Initial Catalog）</item>
    ///   <item>凭据（User+Password）或 集成认证（Integrated Security / Trusted_Connection）</item>
    /// </list>
    /// 兼容 PostgreSQL、MySQL、SQL Server 等常见驱动的键名写法。
    /// </summary>
    private static bool ConnectionStringContainsRequiredKeys(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return false;

        try
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cs };

            var keys = builder.Keys
                .Cast<string>()
                .Select(k => k.ToLowerInvariant())
                .ToHashSet();

            // 主机 / 服务器
            bool hasHost = keys.Any(k =>
                k.Contains("host") || k.Contains("server") || k.Contains("data source"));

            // 数据库名
            bool hasDatabase = keys.Any(k =>
                k.Contains("database") || k.Contains("initial catalog"));

            // 用户凭据
            bool hasUser = keys.Any(k =>
                k.Contains("user") || k.Contains("uid") || k.Contains("username"));
            bool hasPassword = keys.Any(k =>
                k.Contains("password") || k.Contains("pwd"));

            // 集成认证（Windows 身份验证）
            bool hasIntegrated = keys.Any(k =>
                k.Contains("integrated security") || k.Contains("trusted_connection"));

            return hasHost && hasDatabase && ((hasUser && hasPassword) || hasIntegrated);
        }
        catch
        {
            return false;
        }
    }
}
