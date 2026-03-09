using IndustrialDataProcessor.Api.BackgroundServices;
using IndustrialDataProcessor.Api.Middleware;
using IndustrialDataProcessor.Application;
using IndustrialDataProcessor.Infrastructure;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar;
namespace IndustrialDataProcessor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 注册内存缓存 (如果还没注册的话)
        builder.Services.AddMemoryCache();

        // Add services to the container.
        // 注册应用层和基础设施层
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();
        // 注册持久化存储层
        builder.Services.AddPostgreSqlPersistence(builder.Configuration);

        // 在注册应用层和基础设施层注册后台托管服务！
        builder.Services.AddHostedService<DataCollectionHostedService>();

        builder.Services.AddHealthChecks();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 注册异常处理
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // 1. 请求日志（最先）
        app.UseMiddleware<RequestLoggingMiddleware>();
        // 2. 异常处理
        app.UseExceptionHandler();


        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapHealthChecks("/health");
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
