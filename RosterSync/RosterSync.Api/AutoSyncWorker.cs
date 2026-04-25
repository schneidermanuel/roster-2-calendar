using Microsoft.EntityFrameworkCore;
using RosterSync.Model;

namespace RosterSync.Api;

public class AutoSyncWorker(IServiceProvider provider, WorkerQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();

            var configsToRun = await db.SyncConfigs
                .Where(config =>
                    config.IsActive
                )
                .ToListAsync(stoppingToken);
            foreach (var syncConfig in configsToRun)
            {
                queue.Enqueue(syncConfig.Id);
            }
        }
    }
}