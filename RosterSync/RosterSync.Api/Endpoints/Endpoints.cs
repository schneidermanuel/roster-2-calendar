using Microsoft.AspNetCore.Mvc;
using RosterSync.Core;
using RosterSync.Model.Entities;

namespace RosterSync.Api.Endpoints;

public class Endpoints
{
    public static Delegate TestScraper() => async (CancellationToken cancellationToken,
            [FromServices] IEnumerable<IRosterScraper> scrapers, [FromQuery] string url,
            [FromQuery] LinkType type = LinkType.Html) =>
        Results.Ok(await scrapers.Single(s => s.SupportedType == type).ScrapeAsync(url, cancellationToken));
}
