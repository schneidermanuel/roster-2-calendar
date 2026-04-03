namespace RosterSync.Core;

public interface IRosterScraper
{
    Task<IReadOnlyList<RosterEvent>> ScrapeAsync(string url, CancellationToken cancellationToken = default);
}