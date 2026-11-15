using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const string UnexpectedErrorDetail = "An unexpected error occurred";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var statusCode = ex switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (ex is DomainException)
            logger.LogWarning(
                "Request {RequestPath} rejected with status {StatusCode}: {Reason}",
                http.Request.Path, statusCode, ex.Message);
        else
            logger.LogError(ex, "Request {RequestPath} failed", http.Request.Path);

        http.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = CreateProblemDetails(ex, statusCode)
        });
    }

    private ProblemDetails CreateProblemDetails(Exception ex, int statusCode)
    {
        if (ex is ValidationException validation)
            return new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = statusCode
            };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex is DomainException || environment.IsDevelopment() ? ex.Message : UnexpectedErrorDetail
        };

        if (ex is not DomainException && environment.IsDevelopment())
            problem.Extensions["exception"] = ex.ToString();

        return problem;
    }
}
