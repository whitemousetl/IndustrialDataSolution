using FluentValidation;
using MediatR;

namespace IndustrialDataProcessor.Application.Behaviors;

/// <summary>
///  拦截所有进入 MediatR 的Request(Command/Query)
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            // 执行所有针对该请求的 FluentValidation 验证器
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        // 验证通过，放行到你的 SaveWorkstationConfigCommandHandler
        return await next(cancellationToken);
    }
}
