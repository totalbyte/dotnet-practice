using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Exceptions;

namespace Reviews.Application.Common.Behaviors;

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (ex is not ValidationException and not OperationCanceledException)
        {
            var requestName = typeof(TRequest).Name;

            if (ex is DomainException or ConflictException or NotFoundException)
                logger.LogWarning(
                    "{RequestName} rejected with {ExceptionType}: {Reason}", requestName, ex.GetType().Name, ex.Message);
            else
                logger.LogError(ex, "{RequestName} failed with {ExceptionType} {@Request}", requestName, ex.GetType().Name, request);

            throw;
        }
    }
}
