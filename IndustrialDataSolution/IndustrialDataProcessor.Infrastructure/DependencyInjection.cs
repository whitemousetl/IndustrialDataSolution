using HslCommunication;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.BackgroundServices;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;
using IndustrialDataProcessor.Infrastructure.Repositories;
using IndustrialDataProcessor.Infrastructure.Serialization.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 读取配置并进行 HslCommunication 授权验证
        string? hslAuthCode = configuration["HslCommunication:AuthorizationCode"];

        // 如果未配置验证码，直接抛出异常退出程序
        if (string.IsNullOrWhiteSpace(hslAuthCode))
            throw new InvalidOperationException("启动失败：未在 appsettings.json 中找到有效的 HslCommunication:AuthorizationCode 节点。");

        // 如果验证码存在但授权失败，直接抛出异常退出程序
        if (!Authorization.SetAuthorizationCode(hslAuthCode))
            throw new InvalidOperationException("启动失败：HslCommunication 授权验证未通过，请检查授权码是否正确！");

        // 注册领域仓储 (负责 JSON 解析)
        services.AddScoped<IWorkstationConfigRepository, WorkstationConfigRepository>();

        // 注册 IConnectionManager 的实现（占位/可替换）
        // 视场景选择生命周期；通常连接管理器可以用 Singleton
        services.AddSingleton<IConnectionManager, ConnectionManager>();

        // 注册后台服务设备数据后台主机服务
        services.AddHostedService<EquipmentDataHostingService>();    

        // 3. 注册基础设施层的 OPC UA 托管后台服务
        // 第一步：先将实体服务注册为单例对象，确保全局唯一
        services.AddSingleton<OpcUaHostingService>();
        // 第二步：将领域层的接口映射到这个单例对象（供你的 EventHandler 依赖注入使用）
        services.AddSingleton<IDataPublishServerManager>(sp => sp.GetRequiredService<OpcUaHostingService>());
        // 第三步：将其提取为后台托管任务（供程序启动时底层 Host 调用 ExecuteAsync）
        services.AddHostedService(sp => sp.GetRequiredService<OpcUaHostingService>());

        // 注册EquipmentDataProcessor设备数据处理器
        services.AddSingleton<IEquipmentDataProcessor, EquipmentDataProcessor>();
        // 注册设备数据处理器及其依赖组件
        // 如果这两个类没有内部状态（纯方法），建议注册为 Singleton 以提高性能
        services.AddSingleton<PointExpressionConverter>();
        services.AddSingleton<VirtualPointCalculator>();

        // 自动扫描当前程序集(Infrastructure)中所有继承自 IProtocolDriver 的非抽象类，并注册为单例
        var driverTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IProtocolDriver).IsAssignableFrom(t));

        foreach (var type in driverTypes)
        {
            services.AddSingleton(typeof(IProtocolDriver), type);
        }

        services.AddSingleton(provider =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            // 将基础设施层的转换器加进去
            options.Converters.Add(new ProtocolConfigJsonConverter());

            return options;
        });

        return services;
    }
}
