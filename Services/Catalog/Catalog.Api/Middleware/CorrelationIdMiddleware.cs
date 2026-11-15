using System.Diagnostics;
using Serilog.Context;

namespace Catalog.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    private const int MaxLength = 64;
    private static readonly object ItemKey = new();

    public static string? GetCorrelationId(HttpContext http) => http.Items[ItemKey] as string;

    public async Task InvokeAsync(HttpContext http)
    {
        var supplied = http.Request.Headers[HeaderName].ToString();
        var correlationId = supplied.Length is > 0 and <= MaxLength
            ? supplied
            : Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;

        http.Items[ItemKey] = correlationId;
        http.Response.OnStarting(() =>
        {
            http.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(http);
    }
}
