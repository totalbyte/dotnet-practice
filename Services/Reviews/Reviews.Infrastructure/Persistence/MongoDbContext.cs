using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public sealed class MongoDbContext : IDisposable
{
    public const string ReviewsCollectionName = "reviews";

    private readonly MongoClient _client;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ClusterConfigurator = cluster => cluster.Subscribe(new DiagnosticsActivityEventSubscriber());
        _client = new MongoClient(clientSettings);
        Database = _client.GetDatabase(settings.DatabaseName);
    }

    public IMongoClient Client => _client;

    public IMongoDatabase Database { get; }

    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>(ReviewsCollectionName);

    public void Dispose() => _client.Dispose();
}
