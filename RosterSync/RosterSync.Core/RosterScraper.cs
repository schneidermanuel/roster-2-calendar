using System.Text.Json;
using System.Text.RegularExpressions;

namespace RosterSync.Core;

public class RosterScraper(HttpClient httpClient) : IRosterScraper
{
    private static readonly Regex RosterEventsRegex = new(
        @"window\.rosterEvents\s*=\s*(\[.*?\])\s*;",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<RosterEvent>> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    {
        var html = await httpClient.GetStringAsync(url, cancellationToken);

        var match = RosterEventsRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException($"window.rosterEvents not found at {url}");

        var json = match.Groups[1].Value;

        var events = JsonSerializer.Deserialize<List<RosterEvent>>(json, JsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize rosterEvents");

        return events
            .Where(e => !string.Equals(e.Type, "off", StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}