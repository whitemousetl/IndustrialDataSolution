using IndustrialDataProcessor.Api.BackgroundServices;
using IndustrialDataProcessor.Api.Middleware;
using IndustrialDataProcessor.Application;
using IndustrialDataProcessor.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // ----------------------------------------------------------------
        // Bootstrap Logger：在 DI 容器和 appsettings 加载完成之前，
        // 捕获启动阶段的致命异常，确保程序崩溃时也有日志可查。
        // ----------------------------------------------------------------
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();

        try
        {
            // 将控制台编码设置为 UTF-8，尽早执行（在创建 builder 之前）
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var builder = WebApplication.CreateBuilder(args);

            // ----------------------------------------------------------------
            // 替换 .NET 默认日志系统为 Serilog
            // ----------------------------------------------------------------
            builder.Host.UseSerilog((context, services, cfg) =>
            {
                // 判断是否属于工业设备数据采集相关日志（按 SourceContext 前缀路由）
                static bool IsDataCollectionSource(LogEvent e)
                {
                    if (!e.Properties.TryGetValue("SourceContext", out var src))
                        return false;
                    var s = src.ToString().Trim('"');
                    return s.StartsWith("IndustrialDataProcessor.Application.Services.DataCollection", StringComparison.Ordinal)
                        || s.StartsWith("IndustrialDataProcessor.Infrastructure.BackgroundServices.EquipmentData", StringComparison.Ordinal)
                        || s.StartsWith("IndustrialDataProcessor.Api.BackgroundServices.DataCollection", StringComparison.Ordinal);
                }

                // 统一日志输出模板
                const string OutputTemplate =
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext:l}] [T{ThreadId}] {Message:lj}{NewLine}{Exception}";

                cfg
                    // 从 appsettings.json / appsettings.{Env}.json 的 "Serilog" 节读取最低级别配置
                    .ReadFrom.Configuration(context.Configuration)
                    // 允许从 DI 容器注入自定义 Sink / Enricher
                    .ReadFrom.Services(services)
                    // 丰富日志属性
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .Enrich.WithMachineName()

                    // ── Console Sink：异步写入，输出全部日志 ──────────────────
                    .WriteTo.Async(a => a.Console(outputTemplate: OutputTemplate))

                    // ── 程序运行日志文件（排除数据采集日志）────────────────────
                    // 按天滚动，单文件上限 50 MB，保留最近 30 个文件
                    .WriteTo.Logger(lc => lc
                        .Filter.ByExcluding(IsDataCollectionSource)
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(AppContext.BaseDirectory, "logs/program/program-.log"),
                            rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 50L * 1024 * 1024,
                            retainedFileCountLimit: 30,
                            rollOnFileSizeLimit: true,
                            shared: false,
                            outputTemplate: OutputTemplate)))

                    // ── 工业设备数据采集专属日志文件 ──────────────────────────
                    // 按天滚动，单文件上限 200 MB，保留最近 7 个文件
                    // bufferSize = 50000：高频采集场景加大异步队列，彻底不阻塞采集线程
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(IsDataCollectionSource)
                        .WriteTo.Async(
                            configure: a => a.File(
                                path: Path.Combine(AppContext.BaseDirectory, "logs/datacollection/datacollection-.log"),
                                rollingInterval: RollingInterval.Day,
                                fileSizeLimitBytes: 200L * 1024 * 1024,
                                retainedFileCountLimit: 7,
                                rollOnFileSizeLimit: true,
                                shared: false,
                                outputTemplate: OutputTemplate),
                            bufferSize: 50000,
                            blockWhenFull: false)); // 队列满时丢弃而非阻塞，优先保障采集实时性
            });

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
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序异常终止");
            throw;
        }
        finally
        {
            // 确保所有异步日志队列在进程退出前全部刷写到磁盘
            Log.CloseAndFlush();
        }
    }
}
