using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var results = await Task.WhenAll(validators.Select(
            v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "{RequestName} failed validation, handler was not invoked: {@Errors}",
                typeof(TRequest).Name,
                failures.Select(f => new { f.PropertyName, f.ErrorMessage }));

            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
