using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Grpc;

public sealed class GrpcServerLoggingInterceptor(ILogger<GrpcServerLoggingInterceptor> logger) : Interceptor
{
    public const string Transport = "grpc";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        // The scope tags everything logged inside the call (cache hits included), so gRPC traffic
        // can be told apart from REST requests in the structured logs.
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Transport"] = Transport,
            ["RpcMethod"] = context.Method
        });
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {RpcMethod} from {Peer} completed with {GrpcStatus} in {ElapsedMs} ms",
                context.Method, context.Peer, StatusCode.OK, ElapsedMs(startedAt));
            return response;
        }
        catch (RpcException ex)
        {
            var level = ex.StatusCode is StatusCode.Internal or StatusCode.Unknown or StatusCode.DataLoss
                ? LogLevel.Error
                : LogLevel.Warning;
            logger.Log(level, ex, "gRPC {RpcMethod} from {Peer} failed with {GrpcStatus} in {ElapsedMs} ms: {StatusDetail}",
                context.Method, context.Peer, ex.StatusCode, ElapsedMs(startedAt), ex.Status.Detail);
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("gRPC {RpcMethod} from {Peer} cancelled by the client after {ElapsedMs} ms",
                context.Method, context.Peer, ElapsedMs(startedAt));
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC {RpcMethod} from {Peer} failed with {GrpcStatus} in {ElapsedMs} ms: {ExceptionType}",
                context.Method, context.Peer, StatusCode.Unknown, ElapsedMs(startedAt), ex.GetType().Name);
            throw;
        }
    }

    private static long ElapsedMs(long startedAt) => (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}
