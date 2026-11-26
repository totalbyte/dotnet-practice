var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var licenseKey = builder.Configuration["Parameters:luckypenny-license-key"] is { Length: > 0 }
    ? builder.AddParameter("luckypenny-license-key", secret: true)
    : null;

var scriptsDirectory = Path.Combine(builder.AppHostDirectory, "..", "db", "p1");
var creationScript = string.Join(
    Environment.NewLine + "GO" + Environment.NewLine,
    new[] { "schema.sql", "procedures.sql", "seed.sql" }
        .Select(file => File.ReadAllText(Path.Combine(scriptsDirectory, file))));

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithImageTag("2025-latest")
    .WithDataVolume();

var repairsDb = sql.AddDatabase("RepairsDb")
    .WithCreationScript(creationScript);

var catalogDb = sql.AddDatabase("CatalogDb");

var mongo = builder.AddMongoDB("mongo")
    .WithImageTag("9.0")
    .WithDataVolume();

var reviewsDb = mongo.AddDatabase("ReviewsDb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var repairs = builder.AddProject<Projects.Repairs_Api>("repairs-api")
    .WithReference(repairsDb)
    .WaitFor(repairsDb)
    .WithHttpHealthCheck("/health/ready")
    .WithLicenseKey("AutoMapper__LicenseKey", licenseKey);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health/ready")
    .WithLicenseKey("AutoMapper__LicenseKey", licenseKey);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb)
    .WithHttpHealthCheck("/health/ready")
    .WithLicenseKey("LuckyPenny__LicenseKey", licenseKey);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(repairs).WaitFor(repairs)
    .WithReference(catalog).WaitFor(catalog)
    .WithReference(reviews).WaitFor(reviews)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(repairs)
    .WithReference(catalog)
    .WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health/ready", endpointName: "public");

builder.Build().Run();

internal static class LicenseKeyExtensions
{
    public static IResourceBuilder<ProjectResource> WithLicenseKey(
        this IResourceBuilder<ProjectResource> project, string setting, IResourceBuilder<ParameterResource>? key)
        => key is null ? project : project.WithEnvironment(setting, key);
}
