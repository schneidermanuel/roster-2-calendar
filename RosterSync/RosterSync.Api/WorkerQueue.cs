using System.Threading.Channels;

namespace RosterSync.Api;

public class WorkerQueue
{
    private readonly Channel<int> _queue = Channel.CreateUnbounded<int>();

    public void Enqueue(int configId)
    {
        _queue.Writer.TryWrite(configId);
    }

    public async Task<int> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}