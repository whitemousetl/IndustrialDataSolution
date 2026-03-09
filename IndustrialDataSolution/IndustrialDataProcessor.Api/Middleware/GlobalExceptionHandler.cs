using FluentValidation;
using IndustrialDataProcessor.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialDataProcessor.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // 记录异常日志
        if (exception is ArgumentException)
            _logger.LogWarning("请求参数验证失败 - Path: {Path}, Message: {Message}", httpContext.Request.Path, exception.Message);
        else
            _logger.LogError(exception, "发生未处理的异常 - Path: {Path}, Method: {Method}, Message: {Message}",
                httpContext.Request.Path, httpContext.Request.Method, exception.Message);

        // 构建 ProblemDetails 响应
        var problem = exception switch
        {
            // 400 — FluentValidation 验证错误 (提取详细错误字典)
            ValidationException ex => CreateValidationProblem(ex, httpContext),

            // 400 — 参数错误
            ArgumentNullException ex => CreateProblem(400, "参数缺失", ex.Message, httpContext),
            ArgumentException ex => CreateProblem(400, "参数错误", ex.Message, httpContext),

            // 409 — 业务规则错误
            DomainException ex => CreateProblem(409, "业务规则冲突", ex.Message, httpContext),

            // 500 — 用例执行失败 (已修改为最新的 AppServiceException)
            AppServiceException ex => CreateProblem(500, "应用服务执行失败", ex.Message, httpContext),

            // 503 — 基础设施故障 (明确使用 Domain.Exceptions 下的异常)
            InfrastructureException => CreateProblem(503, "基础设施不可用", "数据库或外部服务不可用", httpContext),

            // 500 — 未知错误
            _ => CreateProblem(500, "服务器内部错误", "发生未知异常", httpContext)
        };

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail, HttpContext ctx)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };
    }

    // 专门处理 FluentValidation 异常，生成标准的 RFC 7807 验证错误格式
    private static ProblemDetails CreateValidationProblem(ValidationException ex, HttpContext ctx)
    {
        var problem = new ProblemDetails
        {
            Status = 400,
            Title = "数据验证失败",
            Detail = "提交的数据不符合验证规则，请检查后重试",
            Instance = ctx.Request.Path,
            Type = "https://httpstatuses.com/400"
        };

        // 如果异常中包含具体的 ValidationFailure 集合，将其格式化为字典返回给前端
        if (ex.Errors != null && ex.Errors.Any())
        {
            var errors = ex.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            problem.Extensions.Add("errors", errors);
        }
        else
        {
            // 兼容你之前直接 throw new ValidationException("字符串") 的写法
            problem.Extensions.Add("errors", new Dictionary<string, string[]> { { "General", new[] { ex.Message } } });
        }

        return problem;
    }
}
