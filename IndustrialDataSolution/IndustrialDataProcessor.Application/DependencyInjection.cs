using FluentValidation;
using IndustrialDataProcessor.Application.Behaviors;
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
        // 注册 FluentValidation 验证器，扫描程序集，把所有 AbstractValidator<T> 注册为 IValidator<T>
        // ① 扫描程序集，把所有 AbstractValidator<T> 注册为 IValidator<T>
        services.AddValidatorsFromAssemblyContaining<WorkstationConfigDtoValidator>();
        
        // 必须为 AddScoped，因为它的依赖项 Repository 是 Scoped 的
        services.AddScoped<IDataCollectionAppService, DataCollectionAppService>();
        services.AddSingleton<ICollectionTaskManager, CollectionTaskManager>();
        
        // 【新增】注册数据采集结果的单例通道 (进程内消息总线)
        services.AddSingleton<DataCollectionChannel>();
        
        // 注册 MediatR
        // 【重要】明确指定只扫描 Application 程序集，避免重复注册
        services.AddMediatR(cfg =>
        {
            // 只扫描当前程序集，不使用 RegisterServicesFromAssemblyContaining 以避免潜在的重复扫描问题
            // ② 把 ValidationBehavior 注册为 MediatR 管道拦截器
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // 加入全局验证拦截器，把 ValidationBehavior 注册为 MediatR 管道拦截器
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}