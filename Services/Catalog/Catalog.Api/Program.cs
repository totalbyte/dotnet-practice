using System.Text.Json.Serialization;
using Catalog.Api.ExceptionHandling;
using Catalog.Api.Grpc;
using Catalog.Api.HealthChecks;
using Catalog.Api.Metrics;
using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Validation;
using Catalog.Dal;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Seeding;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var cacheOptions = builder.Configuration.GetSection(CatalogCacheOptions.SectionName).Get<CatalogCacheOptions>()
    ?? new CatalogCacheOptions();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = cacheOptions.MemorySizeLimitKilobytes;
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
    options.TrackStatistics = true;
});
builder.AddRedisDistributedCache("redis",
    settings => settings.DisableHealthChecks = true,
    options =>
    {
        options.AsyncTimeout = 1000;
        options.SyncTimeout = 1000;
    });
builder.Services.AddCatalogServicesWithCaching(builder.Configuration);

builder.Services.AddCatalogMetrics();

builder.Services.AddCatalogHealthChecks(connectionString);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<CatalogItemProfile>();
    cfg.AddProfile<CategoryProfile>();
    cfg.AddProfile<BrandProfile>();
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateCatalogItemDtoValidator>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["correlationId"] =
        CorrelationIdMiddleware.GetCorrelationId(context.HttpContext));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.WebHost.ConfigureKestrel(kestrel => kestrel
    .Configure(builder.Configuration.GetSection("Kestrel"), reloadOnChange: true)
    .Endpoint("grpc", endpoint => endpoint.ListenOptions.Protocols = HttpProtocols.Http2));

builder.Services
    .AddGrpc(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddServerLogging();
builder.Services.AddGrpcHealthChecks(options =>
    options.Services.Map(string.Empty, check => check.Tags.Contains(Extensions.ReadyTag)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
    if (!app.Environment.IsProduction())
        await DbInitializer.SeedAsync(db);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
