using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.ServiceDefaults.Grpc;

public static class GrpcServerLoggingExtensions
{
    public static IGrpcServerBuilder AddServerLogging(this IGrpcServerBuilder grpc)
    {
        grpc.Services.Configure<GrpcServiceOptions>(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());
        return grpc;
    }
}
