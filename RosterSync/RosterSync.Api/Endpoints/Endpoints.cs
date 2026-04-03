using Microsoft.AspNetCore.Mvc;
using RosterSync.Core;

namespace RosterSync.Api.Endpoints;

public class Endpoints
{
    public static Delegate TestScraper() => async (CancellationToken cancellationToken,
            [FromServices] IRosterScraper scraper, [FromQuery] string url) =>
        await scraper.ScrapeAsync(url, cancellationToken);
}