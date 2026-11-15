var builder = DistributedApplication.CreateBuilder(args);

var scriptsDirectory = Path.Combine(builder.AppHostDirectory, "..", "db", "p1");
var creationScript = string.Join(
    Environment.NewLine + "GO" + Environment.NewLine,
    new[] { "schema.sql", "procedures.sql", "seed.sql" }
        .Select(file => File.ReadAllText(Path.Combine(scriptsDirectory, file))));

var sql = builder.AddSqlServer("sql")
    .WithImageTag("2025-latest")
    .WithDataVolume();

var repairsDb = sql.AddDatabase("RepairsDb")
    .WithCreationScript(creationScript);

builder.AddProject<Projects.Repairs_Api>("repairs-api")
    .WithReference(repairsDb)
    .WaitFor(repairsDb);

var catalogDb = sql.AddDatabase("CatalogDb");

builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

var mongo = builder.AddMongoDB("mongo")
    .WithImageTag("9.0")
    .WithDataVolume();

var reviewsDb = mongo.AddDatabase("reviews");

builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithEnvironment("MongoDb__ConnectionString", reviewsDb.Resource.ConnectionStringExpression)
    .WithEnvironment("MongoDb__DatabaseName", reviewsDb.Resource.DatabaseName)
    .WaitFor(reviewsDb);

builder.Build().Run();
