using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults.Correlation;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";
    public const string ActivityTagName = "correlation.id";

    private const int MaxLength = 64;

    public static string? GetCorrelationId(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        return value.Length > 0 ? value : null;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        var isGenerated = !IsValid(supplied);
        var correlationId = isGenerated ? Guid.NewGuid().ToString() : supplied;

        context.Request.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag(ActivityTagName, correlationId);

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            if (IsProbe(context.Request.Path))
            {
                await next(context);
                return;
            }

            if (isGenerated)
                logger.LogInformation("Generated correlation id {CorrelationId} for {Method} {Path}",
                    correlationId, context.Request.Method, context.Request.Path);

            var startedAt = Stopwatch.GetTimestamp();
            logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Request finished {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode,
                    (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
        }
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static bool IsProbe(PathString path) => path.StartsWithSegments(Extensions.HealthEndpointPath);
}
