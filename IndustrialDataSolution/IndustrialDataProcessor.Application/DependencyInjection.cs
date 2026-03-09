using FluentValidation;
using IndustrialDataProcessor.Application.Behaviors;
using IndustrialDataProcessor.Application.Commands;
using IndustrialDataProcessor.Application.Services;
using IndustrialDataProcessor.Application.Validators;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialDataProcessor.Application;

public static class DependencyInjection
{
    /// <summary>
    /// 注册应用服务层服务
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 注册应用服务
        // 未来可以添加更多服务
        // services.AddScoped<OtherService>();
         services.AddValidatorsFromAssemblyContaining<WorkstationConfigDtoValidator>();
        // 必须为 AddScoped，因为它的依赖项 Repository 是 Scoped 的
        services.AddScoped<IDataCollectionAppService, DataCollectionAppService>();
        services.AddSingleton<ICollectionTaskManager, CollectionTaskManager>();
        // 【新增】注册数据采集结果的单例通道 (进程内消息总线)
        services.AddSingleton<DataCollectionChannel>();
        // 注册 MediatR（将扫描包含命令/处理器的程序集中定义的 handler）
        // 确保项目已引用 MediatR.Extensions.Microsoft.DependencyInjection
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SaveWorkstationConfigCommand>());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<SaveWorkstationConfigCommand>();

            //加入全局验证拦截器
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}