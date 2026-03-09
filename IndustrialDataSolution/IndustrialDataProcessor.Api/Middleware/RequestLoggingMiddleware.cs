using System.Diagnostics;
using System.Text;

namespace IndustrialDataProcessor.Api.Middleware;

/// <summary>
/// 请求日志中间件
/// </summary>
public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // 开始计时
        var stopwatch = Stopwatch.StartNew();

        // 记录请求信息
        var requestInfo = await GetRequestInfoAsync(context.Request);
        _logger.LogInformation(
            "开始处理请求: {Method} {Path} - TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        // 可选：记录请求体（注意性能影响）
        if (_logger.IsEnabled(LogLevel.Debug) && ShouldLogRequestBody(context.Request))
            _logger.LogDebug("请求体: {RequestBody}", requestInfo.Body);

        // 保存原始响应流
        var originalBodyStream = context.Response.Body;

        try
        {
            // 使用内存流拦截响应
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // 执行下一个中间件
            await _next(context);

            // 停止计时
            stopwatch.Stop();

            // 记录响应信息
            _logger.LogInformation(
                "完成请求: {Method} {Path} - 状态码: {StatusCode} - 耗时: {ElapsedMs}ms - TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);

            // 可选：记录响应体（注意性能影响）
            if (_logger.IsEnabled(LogLevel.Debug) && ShouldLogResponseBody(context.Response))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                _logger.LogDebug("响应体: {ResponseBody}", responseBodyText);
            }

            // 将响应写回原始流
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "请求处理异常: {Method} {Path} - 耗时: {ElapsedMs}ms - TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);
            throw; // 继续抛出异常，让异常处理中间件处理
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// 获取请求信息
    /// </summary>
    private static async Task<RequestInfo> GetRequestInfoAsync(HttpRequest request)
    {
        var info = new RequestInfo
        {
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        };

        // 读取请求体（需要启用缓冲）
        if (ShouldLogRequestBody(request))
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            info.Body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // 重置流位置
        }

        return info;
    }

    /// <summary>
    /// 判断是否应该记录请求体
    /// </summary>
    private static bool ShouldLogRequestBody(HttpRequest request)
    {
        // 只记录 POST/PUT/PATCH 的 JSON 请求
        return (request.Method == HttpMethod.Post.Method ||
                request.Method == HttpMethod.Put.Method ||
                request.Method == HttpMethod.Patch.Method) &&
               request.ContentType?.Contains("application/json") == true;
    }

    /// <summary>
    /// 判断是否应该记录响应体
    /// </summary>
    private static bool ShouldLogResponseBody(HttpResponse response)
    {
        // 只记录成功的 JSON 响应
        return response.StatusCode < 400 &&
               response.ContentType?.Contains("application/json") == true;
    }

    private record RequestInfo
    {
        public string Method { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string QueryString { get; init; } = string.Empty;
        public Dictionary<string, string> Headers { get; init; } = [];
        public string? Body { get; set; }
    }
}