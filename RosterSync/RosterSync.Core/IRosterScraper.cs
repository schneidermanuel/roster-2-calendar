using RosterSync.Model.Entities;

namespace RosterSync.Core;

public interface IRosterScraper
{
    LinkType SupportedType { get; }
    Task<IReadOnlyList<RosterEvent>> ScrapeAsync(string url, CancellationToken cancellationToken = default);
}
