using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Repairs.Domain.Exceptions;

namespace Repairs.Api.ExceptionHandling;

public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedErrorDetail = "An unexpected error occurred";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var statusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (ex is DomainException)
            logger.LogWarning("Request rejected with status {StatusCode}: {Reason}", statusCode, ex.Message);
        else
            logger.LogError(ex, "Unhandled exception");

        http.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = ex is DomainException ? ex.Message : UnexpectedErrorDetail
            }
        });
    }
}
