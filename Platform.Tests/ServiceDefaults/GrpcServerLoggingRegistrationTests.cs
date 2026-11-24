using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.ServiceDefaults.Grpc;

namespace Platform.Tests.ServiceDefaults;

public sealed class GrpcServerLoggingRegistrationTests
{
    [Fact]
    public void Server_logging_adds_the_interceptor_exactly_once()
    {
        var services = new ServiceCollection();
        services.AddGrpc().AddServerLogging();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value;

        Assert.Single(options.Interceptors, registration => registration.Type == typeof(GrpcServerLoggingInterceptor));
    }

    [Fact]
    public void Server_logging_keeps_other_grpc_options()
    {
        var services = new ServiceCollection();
        services.AddGrpc(options => options.EnableDetailedErrors = true).AddServerLogging();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value;

        Assert.True(options.EnableDetailedErrors);
        Assert.Single(options.Interceptors);
    }
}
