using HslCommunication;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.BackgroundServices;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;
using IndustrialDataProcessor.Infrastructure.Persistence.Repositories;
using IndustrialDataProcessor.Infrastructure.Repositories;
using IndustrialDataProcessor.Infrastructure.Serialization.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// 注册基础设施层服务
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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

        // 7. 自动注册协议驱动
        RegisterProtocolDrivers(services);

        // 8. 配置 JSON 序列化选项
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
        // 领域仓储（负责 JSON 解析）
        services.AddScoped<IWorkstationConfigRepository, WorkstationConfigRepository>();
        
        // 实体仓储（负责数据库操作）
        services.AddScoped<IWorkstationConfigEntityRepository, WorkstationConfigEntityRepository>();
        
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
    private static void ConfigureJsonOptions(IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            options.Converters.Add(new ProtocolConfigJsonConverter());

            return options;
        });
    }
}
