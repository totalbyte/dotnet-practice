using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reviews.Infrastructure.Seeding;

public sealed class DataSeedingService(
    IServiceScopeFactory scopeFactory, IHostEnvironment environment, ILogger<DataSeedingService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            logger.LogInformation("Seeding is disabled in the {Environment} environment", environment.EnvironmentName);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
            await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
