using HslCommunication;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.BackgroundServices;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;
using IndustrialDataProcessor.Infrastructure.OpcUa;
using IndustrialDataProcessor.Infrastructure.Persistence.Repositories;
using IndustrialDataProcessor.Infrastructure.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// 注册基础设施层服务
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 0.绑定 OPC UA 配置选项
        services.Configure<OpcUaOptions>(configuration.GetSection(OpcUaOptions.SectionName));

        // 1. 配置 SqlSugar 数据库客户端
        ConfigureSqlSugar(services, configuration);

        // 2. 读取配置并进行 HslCommunication 授权验证
        ConfigureHslCommunication(configuration);

        // 3. 注册仓储服务
        RegisterRepositories(services);

        // 4. 注册连接管理器
        services.AddSingleton<IConnectionManager, ConnectionManager>();

        // 5. 注册后台服务
        RegisterBackgroundServices(services);

        // 6. 注册设备数据处理器
        RegisterDataProcessors(services);

        // 7. 注册配置缓存服务
        RegisterConfigCache(services);

        // 8. 自动注册协议驱动
        RegisterProtocolDrivers(services);

        // 9. 配置 JSON 序列化选项
        ConfigureJsonOptions(services);

        return services;
    }

    /// <summary>
    /// 配置 SqlSugar 数据库客户端
    /// </summary>
    private static void ConfigureSqlSugar(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("数据库连接字符串未配置");

        services.AddTransient<ISqlSugarClient>(provider =>
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                MoreSettings = new ConnMoreSettings
                {
                    PgSqlIsAutoToLower = false,
                }
            });

            return db;
        });
    }

    /// <summary>
    /// 配置 HslCommunication 授权
    /// </summary>
    private static void ConfigureHslCommunication(IConfiguration configuration)
    {
        string? hslAuthCode = configuration["HslCommunication:AuthorizationCode"];

        if (string.IsNullOrWhiteSpace(hslAuthCode))
            throw new InvalidOperationException("启动失败：未在 appsettings.json 中找到有效的 HslCommunication:AuthorizationCode 节点。");

        if (!Authorization.SetAuthorizationCode(hslAuthCode))
            throw new InvalidOperationException("启动失败：HslCommunication 授权验证未通过，请检查授权码是否正确！");
    }

    /// <summary>
    /// 注册仓储服务
    /// </summary>
    private static void RegisterRepositories(IServiceCollection services)
    {
        // 领域仓储（负责 JSON 解析和领域模型转换）
        services.AddScoped<IWorkstationConfigRepository, WorkstationConfigRepository>();
        
        // 持久化仓储（负责数据库 CRUD 操作）
        services.AddScoped<IWorkstationConfigPersistenceRepository, WorkstationConfigPersistenceRepository>();
        
        // 设备数据存储仓储
        services.AddSingleton<IEquipmentDataStorageRepository, EquipmentDataStorageRepository>();
    }

    /// <summary>
    /// 注册后台服务
    /// </summary>
    private static void RegisterBackgroundServices(IServiceCollection services)
    {
        // 设备数据后台主机服务
        services.AddHostedService<EquipmentDataHostingService>();

        // OPC UA 托管后台服务
        services.AddSingleton<OpcUaHostingService>();
        services.AddSingleton<IDataPublishServerManager>(sp => sp.GetRequiredService<OpcUaHostingService>());
        services.AddHostedService(sp => sp.GetRequiredService<OpcUaHostingService>());
    }

    /// <summary>
    /// 注册设备数据处理器
    /// </summary>
    private static void RegisterDataProcessors(IServiceCollection services)
    {
        services.AddSingleton<IEquipmentDataProcessor, EquipmentDataProcessor>();
        services.AddSingleton<PointExpressionConverter>();
        services.AddSingleton<VirtualPointCalculator>();
    }

    /// <summary>
    /// 注册配置缓存服务
    /// </summary>
    private static void RegisterConfigCache(IServiceCollection services)
    {
        // 注册为单例，确保全局共享同一缓存实例
        services.AddSingleton<IWorkstationConfigCache, WorkstationConfigCache>();
    }

    /// <summary>
    /// 自动注册协议驱动
    /// </summary>
    private static void RegisterProtocolDrivers(IServiceCollection services)
    {
        var driverTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IProtocolDriver).IsAssignableFrom(t));

        foreach (var type in driverTypes)
        {
            services.AddSingleton(typeof(IProtocolDriver), type);
        }
    }

    /// <summary>
    /// 配置 JSON 序列化选项
    /// </summary>
    //private static void ConfigureJsonOptions(IServiceCollection services)
    //{
    //    services.AddSingleton(provider =>
    //    {
    //        var options = new JsonSerializerOptions
    //        {
    //            PropertyNamingPolicy = null,
    //            PropertyNameCaseInsensitive = true,
    //            WriteIndented = false
    //        };

    //        options.Converters.Add(new ProtocolConfigPolymorphicConverter());

    //        return options;
    //    });
    //}

    /// <summary>
    /// 配置全局 JSON 序列化/反序列化选项，供依赖注入容器使用。
    /// 这些选项将应用于整个应用程序中通过 HttpClient、Controller 或手动序列化的所有 JSON 操作。
    /// </summary>
    //private static void ConfigureJsonOptions(IServiceCollection services)
    //{
    //    services.AddSingleton(provider =>
    //    {
    //        var options = new JsonSerializerOptions
    //        {
    //            // 1. 命名策略：保留 C# 属性原始名称（PascalCase）
    //            //    若对接的外部系统要求 camelCase 或其他命名风格，可设置为 JsonNamingPolicy.CamelCase 等。
    //            PropertyNamingPolicy = null,

    //            // 2. 大小写不敏感：反序列化时允许属性名大小写不一致，增强容错性。
    //            //    在工业环境中，来自不同设备或系统的 JSON 大小写可能不一致，建议保留为 true。
    //            PropertyNameCaseInsensitive = true,

    //            // 3. 缩进格式：设置为 false 以减小传输体积，提升性能。
    //            //    仅当需要人工阅读日志或调试时，可临时改为 true。
    //            WriteIndented = false,

    //            // 4. 字符编码：使用宽松的编码策略，避免非 ASCII 字符（如中文）被转义为 \uxxxx，
    //            //    提高可读性并减少序列化开销。注意：在内网工业环境中 XSS 风险可忽略，
    //            //    若输出到 Web 前端，需评估安全性。
    //            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

    //            // 5. 忽略默认值：序列化时忽略值为 null 的属性，减少 JSON 体积。
    //            //    若业务逻辑要求必须包含 null 字段（如某些旧系统要求字段始终存在），
    //            //    可改为 JsonIgnoreCondition.Never 或移除该配置。
    //            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

    //            // 6. 数字处理：使用严格模式，要求 JSON 中的数字必须为原始数字格式（不加引号）。
    //            //    若对接的老旧系统可能将数字表示为字符串（如 "123.45"），则需要改为
    //            //    JsonNumberHandling.AllowReadingFromString 或更宽松的组合。
    //            NumberHandling = JsonNumberHandling.Strict,

    //            // 7. 最大嵌套深度：防止解析超深 JSON 导致栈溢出或 DoS 攻击。
    //            //    根据实际数据结构的最大深度设置，64 通常足够安全且不会限制正常数据。
    //            MaxDepth = 64
    //        };

    //        // 添加常用转换器（Converters）
    //        // 转换器的顺序会影响匹配优先级，通常将特定转换器放在前面。

    //        // 8. 枚举转字符串转换器：默认枚举序列化为数字，但工业协议中可读性更重要，
    //        //    使用字符串可避免数字含义混淆。若需自定义枚举格式（如驼峰、全大写），
    //        //    可传入 JsonStringEnumConverter 的参数。
    //        options.Converters.Add(new JsonStringEnumConverter());

    //        // 9. 自定义多态转换器：处理协议配置类的继承/接口多态。
    //        //    该转换器必须根据实际类型标识（如 "$type" 字段）正确反序列化为派生类型，
    //        //    是应对工业协议中多种设备类型/协议配置的关键。
    //        options.Converters.Add(new ProtocolConfigPolymorphicConverter());

    //        // 10. （可选）日期时间转换器：若需要统一时间格式（如 Unix 毫秒时间戳或 ISO8601），
    //        //     可添加自定义转换器，确保与上下游系统时间格式一致。
    //        // options.Converters.Add(new CustomDateTimeConverter());

    //        // 11. （可选）源生成器上下文：若使用 System.Text.Json 源生成器（.NET 6+），
    //        //     可在此指定生成的上下文，以消除反射、提升启动性能。
    //        // options.TypeInfoResolver = MyJsonContext.Default;

    //        return options;
    //    });
    //}

    /// <summary>
    /// 配置 DI 容器中的 JSON 序列化选项单例。
    /// <para>供 Handler / Repository 等基础设施层组件手动序列化使用。</para>
    /// <para>额外注册 ProtocolConfigPolymorphicConverter 用于领域模型的多态反序列化。</para>
    /// </summary>
    private static void ConfigureJsonOptions(IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var options = new JsonSerializerOptions();

            // 应用公共配置
            ApplyCommonJsonSettings(options);

            // DI 单例专用：领域模型多态转换器（控制器不需要此转换器）
            options.Converters.Add(new ProtocolConfigPolymorphicConverter());

            return options;
        });
    }

    /// <summary>
    /// 应用公共 JSON 序列化配置。
    /// <para>供 DI 单例和 ASP.NET Core 控制器共享，确保全局行为一致。</para>
    /// </summary>
    public static void ApplyCommonJsonSettings(JsonSerializerOptions options)
    {
        // 命名策略：保留 PascalCase
        options.PropertyNamingPolicy = null;

        // 大小写不敏感：增强工业环境中不同系统的兼容性
        options.PropertyNameCaseInsensitive = true;

        // 紧凑格式：减小传输体积
        options.WriteIndented = false;

        // 宽松编码：中文等非 ASCII 字符不转义为 \uxxxx
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        // 忽略 null：减少 JSON 体积
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // 严格数字：禁止 "123" 字符串形式的数字
        options.NumberHandling = JsonNumberHandling.Strict;

        // 最大深度：防止深层 JSON 导致栈溢出
        options.MaxDepth = 64;

        // 枚举序列化为字符串：提升可读性
        //options.Converters.Add(new JsonStringEnumConverter());
    }
}
