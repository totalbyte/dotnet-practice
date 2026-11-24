using System.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Repairs.Api.ExceptionHandling;
using Repairs.Api.Grpc;
using Repairs.Bll.Interfaces;
using Repairs.Bll.Mapping;
using Repairs.Bll.Services;
using Repairs.Dal;
using Repairs.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("RepairsDb")
    ?? throw new InvalidOperationException("Connection string 'RepairsDb' is missing");

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICatalogItemService, CatalogItemService>();
builder.Services.AddScoped<IRepairOrderService, RepairOrderService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<CustomerProfile>();
    cfg.AddProfile<CatalogItemProfile>();
    cfg.AddProfile<RepairOrderProfile>();
});

builder.Services.AddControllers();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] =
        Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.WebHost.ConfigureKestrel(kestrel => kestrel
    .Configure(builder.Configuration.GetSection("Kestrel"), reloadOnChange: true)
    .Endpoint("grpc", endpoint => endpoint.ListenOptions.Protocols = HttpProtocols.Http2));

builder.Services
    .AddGrpc(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddServerLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<RepairsGrpcService>();
app.MapDefaultEndpoints();

app.Run();
