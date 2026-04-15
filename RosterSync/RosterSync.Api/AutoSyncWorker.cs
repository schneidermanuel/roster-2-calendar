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
            var now = DateTime.Now;
            var startOfDay = now.Date;
            var endOfDay = startOfDay.AddDays(1);
            var currentTime = TimeOnly.FromDateTime(now);

            var configsToRun = await db.SyncConfigs
                .Where(config =>
                    config.DailyTriggerTime <= currentTime &&
                    config.IsActive &&
                    !db.SyncLogs.Any(log =>
                        log.SyncConfigId == config.Id &&
                        log.StartedAt >= startOfDay &&
                        log.StartedAt < endOfDay
                    )
                )
                .ToListAsync(stoppingToken);
            foreach (var syncConfig in configsToRun)
            {
                queue.Enqueue(syncConfig.Id);
            }
        }
    }
}