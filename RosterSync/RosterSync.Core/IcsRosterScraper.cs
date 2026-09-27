using System.Text.RegularExpressions;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using RosterSync.Model.Entities;

namespace RosterSync.Core;

public class IcsRosterScraper() : IRosterScraper
{
    private static readonly Regex FlightSummaryRegex = new(
        @"^(?<flight>\S+)\s+\S{4}\((?<origin>[A-Z]{3})\)-\S{4}\((?<destination>[A-Z]{3})\)(,\s*(?<aircraft>[^,]+))?",
        RegexOptions.Compiled);

    private static readonly Regex AirportSuffixRegex = new(
        @"\S{4}\((?<iata>[A-Z]{3})\)\s*$",
        RegexOptions.Compiled);

    public LinkType SupportedType => LinkType.Ics;

    public async Task<IReadOnlyList<RosterEvent>> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var httpClient = new HttpClient();
            await using (var download = await httpClient.GetStreamAsync(url, cancellationToken))
            await using (var file = File.Create(tempFile))
            {
                await download.CopyToAsync(file, cancellationToken);
            }

            var text = await File.ReadAllTextAsync(tempFile, cancellationToken);
            var calendar = Calendar.Load(text)
                           ?? throw new InvalidOperationException($"Failed to parse ICS calendar from {url}");

            return calendar.Events
                .Select(ToRosterEvent)
                .OfType<RosterEvent>()
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static RosterEvent? ToRosterEvent(CalendarEvent e)
    {
        var summary = e.Summary?.Trim() ?? string.Empty;

        // Check-in is a separate placeholder duty that always precedes its flight; the flight
        // event itself carries the real schedule, so check-in doesn't need to be tracked.
        if (summary.Equals("Check in", StringComparison.OrdinalIgnoreCase))
            return null;

        if (e.Start is null)
            throw new InvalidOperationException($"ICS event {e.Uid} has no DTSTART");

        var start = ToUtc(e.Start);
        var end = e.End is null ? start : ToUtc(e.End);
        var status = e.Status ?? "CONFIRMED";
        var createdAt = e.Created is null ? DateTime.UtcNow : ToUtc(e.Created);
        var id = StableHash(e.Uid ?? $"{summary}_{start:O}");

        var flightMatch = FlightSummaryRegex.Match(summary);
        if (flightMatch.Success)
        {
            return new RosterEvent
            {
                Id = id,
                Type = "flight",
                FlightNumber = flightMatch.Groups["flight"].Value,
                Aircraft = flightMatch.Groups["aircraft"].Success ? flightMatch.Groups["aircraft"].Value.Trim() : null,
                Origin = flightMatch.Groups["origin"].Value,
                Destination = flightMatch.Groups["destination"].Value,
                StartTime = start,
                EndTime = end,
                Status = status,
                CreatedAt = createdAt,
                Description = summary
            };
        }

        var airportMatch = AirportSuffixRegex.Match(summary);

        return new RosterEvent
        {
            Id = id,
            Type = GetDutyType(summary),
            Origin = airportMatch.Success ? airportMatch.Groups["iata"].Value : e.Location,
            StartTime = start,
            EndTime = end,
            Status = status,
            CreatedAt = createdAt,
            Description = summary
        };
    }

    private static string GetDutyType(string summary)
    {
        if (summary.StartsWith("Off", StringComparison.OrdinalIgnoreCase))
            return "off";
        if (summary.StartsWith("Standby", StringComparison.OrdinalIgnoreCase))
            return "standby";
        if (summary.StartsWith("Course", StringComparison.OrdinalIgnoreCase))
            return "course";
        if (summary.StartsWith("Krank", StringComparison.OrdinalIgnoreCase))
            return "sick";

        return "duty";
    }

    private static DateTime ToUtc(CalDateTime dateTime)
    {
        if (!dateTime.HasTime)
        {
            return DateTime.SpecifyKind(dateTime.Value.Date, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(dateTime.AsUtc, DateTimeKind.Utc);
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = hash * 31 + c;
            }

            return hash;
        }
    }
}
