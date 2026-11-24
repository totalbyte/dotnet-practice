using System.Text.Json.Serialization;
using Catalog.Api.ExceptionHandling;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validation;
using Catalog.Dal;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults.Correlation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICatalogItemService, CatalogItemService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();

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
app.MapDefaultEndpoints();

app.Run();
