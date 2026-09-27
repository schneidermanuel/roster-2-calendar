using RosterSync.Core;

namespace RosterSync.Api;

public class SyncWorker(IServiceProvider provider, WorkerQueue queue, ILogger<SyncWorker> logger) : BackgroundService
{
    private async Task ExecuteAsync(int configId, CancellationToken cancellationToken)
    {
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RosterSyncService>();
        await service.SyncAsync(configId, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var configId = await queue.DequeueAsync(stoppingToken);
            try
            {
                await ExecuteAsync(configId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single sync config failing (bad URL, unreachable host, malformed feed, ...)
                // must not take down the worker for every other config.
                logger.LogError(ex, "Sync failed for config {ConfigId}", configId);
            }
        }
    }
}