using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.ExceptionHandling;

public sealed class UpstreamUnavailableExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not UpstreamUnavailableException unavailable)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Required service is unavailable",
                Detail = $"{unavailable.Service} is unavailable, the repair order card cannot be built"
            }
        });
    }
}
