using IndustrialDataProcessor.Api.BackgroundServices;
using IndustrialDataProcessor.Api.Middleware;
using IndustrialDataProcessor.Application;
using IndustrialDataProcessor.Infrastructure;
using System.Text;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // 将控制台编码设置为 UTF-8，尽早执行（在创建 builder 之前）
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var builder = WebApplication.CreateBuilder(args);

        // 注册基础设施层（包含持久化、仓储、基础设施后台服务等）
        builder.Services.AddInfrastructure(builder.Configuration);
        
        // 注册应用层服务
        builder.Services.AddApplication();

        // 注册组合根层的后台服务（启动/编排类服务）
        builder.Services.AddHostedService<DataCollectionHostedService>();

        // 注册健康检查和控制器
        builder.Services.AddHealthChecks();
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // 复用 Infrastructure 层的公共 JSON 配置，保证全局一致性
                Infrastructure.DependencyInjection.ApplyCommonJsonSettings(options.JsonSerializerOptions);
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true));
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // 注册异常处理
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // 配置中间件管道
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();

        // 配置 HTTP 请求管道
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapHealthChecks("/health");
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
