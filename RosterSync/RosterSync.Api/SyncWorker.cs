using RosterSync.Core;

namespace RosterSync.Api;

public class SyncWorker(IServiceProvider provider, WorkerQueue queue) : BackgroundService
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
            await ExecuteAsync(configId, stoppingToken);
        }
    }
}