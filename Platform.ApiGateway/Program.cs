using Platform.ApiGateway.Logging;
using Platform.ServiceDefaults.Correlation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddSingleton<GatewayMetrics>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GatewayLoggingMiddleware>();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
