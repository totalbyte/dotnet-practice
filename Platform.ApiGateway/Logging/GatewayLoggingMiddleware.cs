using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Platform.ApiGateway.Logging;

public sealed class GatewayLoggingMiddleware(
    RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger, GatewayMetrics metrics)
{
    private const string NoRoute = "none";

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments(Extensions.HealthEndpointPath))
        {
            await next(context);
            return;
        }

        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("Gateway received {Method} {Target} from {ClientIp}",
            request.Method, request.GetEncodedPathAndQuery(), context.Connection.RemoteIpAddress?.ToString());

        var originalBody = context.Response.Body;
        await using var countingBody = new CountingStream(originalBody);
        context.Response.Body = countingBody;
        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 1);
        var proxy = context.Features.Get<IReverseProxyFeature>();
        var route = proxy?.Route.Config.RouteId ?? NoRoute;
        var cluster = proxy?.Cluster?.Config.ClusterId ?? NoRoute;
        var destination = proxy?.ProxiedDestination?.Model.Config.Address ?? NoRoute;
        var statusCode = context.Response.StatusCode;
        var proxyError = context.Features.Get<IForwarderErrorFeature>();

        if (proxyError is not null)
        {
            logger.LogError(proxyError.Exception,
                "{Method} {Path} -> route {Route}, cluster {Cluster}, destination {Destination} failed with {ProxyError}: {StatusCode} in {ElapsedMs} ms",
                request.Method, request.Path, route, cluster, destination, proxyError.Error, statusCode, elapsedMs);
        }
        else
        {
            logger.Log(statusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Warning : LogLevel.Information,
                "{Method} {Path} -> route {Route}, cluster {Cluster}, destination {Destination}: {StatusCode} in {ElapsedMs} ms, {ResponseBytes} bytes",
                request.Method, request.Path, route, cluster, destination, statusCode, elapsedMs, countingBody.BytesWritten);
        }

        metrics.Record(route, cluster, statusCode, elapsedMs,
            proxyError is not null || statusCode >= StatusCodes.Status500InternalServerError);
    }
}
