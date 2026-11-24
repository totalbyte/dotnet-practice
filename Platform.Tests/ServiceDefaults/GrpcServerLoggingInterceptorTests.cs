using Grpc.Core;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Grpc;

namespace Platform.Tests.ServiceDefaults;

public sealed class GrpcServerLoggingInterceptorTests
{
    private const string Method = "/catalog.CatalogService/GetCatalogItem";

    private readonly RecordingLogger _logger = new();

    [Fact]
    public async Task Logs_method_peer_status_and_duration_of_successful_call()
    {
        var interceptor = new GrpcServerLoggingInterceptor(_logger);

        var response = await interceptor.UnaryServerHandler("request", new TestServerCallContext(Method),
            (_, _) => Task.FromResult("response"));

        Assert.Equal("response", response);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(Method, entry.Properties["RpcMethod"]);
        Assert.Equal("ipv4:127.0.0.1:50000", entry.Properties["Peer"]);
        Assert.Equal(StatusCode.OK, entry.Properties["GrpcStatus"]);
        Assert.True(entry.Properties.ContainsKey("ElapsedMs"));
    }

    [Fact]
    public async Task Logs_status_of_rpc_exception_as_warning_and_rethrows_it()
    {
        var interceptor = new GrpcServerLoggingInterceptor(_logger);

        var exception = await Assert.ThrowsAsync<RpcException>(() => interceptor.UnaryServerHandler<string, string>(
            "request", new TestServerCallContext(Method),
            (_, _) => throw new RpcException(new Status(StatusCode.NotFound, "CatalogItem '99' was not found"))));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(StatusCode.NotFound, entry.Properties["GrpcStatus"]);
        Assert.Equal("CatalogItem '99' was not found", entry.Properties["StatusDetail"]);
    }

    [Fact]
    public async Task Logs_unexpected_exception_as_error()
    {
        var interceptor = new GrpcServerLoggingInterceptor(_logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => interceptor.UnaryServerHandler<string, string>(
            "request", new TestServerCallContext(Method), (_, _) => throw new InvalidOperationException("boom")));

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(StatusCode.Unknown, entry.Properties["GrpcStatus"]);
    }

    [Fact]
    public async Task Opens_scope_that_marks_the_grpc_transport()
    {
        var interceptor = new GrpcServerLoggingInterceptor(_logger);

        await interceptor.UnaryServerHandler("request", new TestServerCallContext(Method),
            (_, _) => Task.FromResult("response"));

        var scope = Assert.Single(_logger.Scopes);
        Assert.Equal("grpc", scope["Transport"]);
        Assert.Equal(Method, scope["RpcMethod"]);
    }

    private sealed class RecordingLogger : ILogger<GrpcServerLoggingInterceptor>
    {
        public List<(LogLevel Level, IReadOnlyDictionary<string, object?> Properties)> Entries { get; } = [];

        public List<IReadOnlyDictionary<string, object>> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IReadOnlyDictionary<string, object> values)
                Scopes.Add(values);
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            Entries.Add((logLevel, properties.ToDictionary(pair => pair.Key, pair => pair.Value)));
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
