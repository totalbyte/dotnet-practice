using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const string DuplicateKeyDetail = "A review with the same unique key already exists";
    private const string WriteRejectedDetail = "The document was rejected by the database";
    private const string UnavailableDetail = "The review storage is temporarily unavailable";
    private const string UnexpectedDetail = "An unexpected error occurred";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var statusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => StatusCodes.Status409Conflict,
            MongoWriteException => StatusCodes.Status400BadRequest,
            MongoConnectionException or TimeoutException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Request {Method} {RequestPath} failed with status {StatusCode}",
                http.Request.Method, http.Request.Path, statusCode);
        else
            logger.LogWarning("Request {Method} {RequestPath} rejected with status {StatusCode}: {Reason}",
                http.Request.Method, http.Request.Path, statusCode, ex.Message);

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
            return new ValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = statusCode
            };

        var isExpected = ex is NotFoundException or DomainException or ConflictException;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex switch
            {
                _ when isExpected => ex.Message,
                MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } => DuplicateKeyDetail,
                MongoWriteException => WriteRejectedDetail,
                MongoConnectionException or TimeoutException => UnavailableDetail,
                _ => UnexpectedDetail
            }
        };

        if (!isExpected && environment.IsDevelopment())
            problem.Extensions["exception"] = ex.ToString();

        return problem;
    }
}
