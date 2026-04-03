using Microsoft.EntityFrameworkCore;
using RosterSync.Model;
using RosterSync.Model.Entities;

namespace RosterSync.Core;

public class RosterSyncService(IDbContext db, IRosterScraper scraper)
{
    public async Task SyncAsync(SyncConfig config, CancellationToken cancellationToken = default)
    {
        var log = new SyncLog
        {
            SyncConfig = config,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };
        db.SyncLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var rosterEvents = await scraper.ScrapeAsync(config.RosterUrl, cancellationToken);

            var dbEvents = await db.SyncedEvents
                .Where(e => e.SyncConfig.Id == config.Id)
                .ToListAsync(cancellationToken);

            var rosterByKey = rosterEvents.ToDictionary(GetNaturalKey);
            var dbByKey = dbEvents.ToDictionary(GetNaturalKey);

            var added = 0;
            var updated = 0;

            foreach (var (key, rosterEvent) in rosterByKey)
            {
                if (dbByKey.TryGetValue(key, out var existing))
                {
                    if (HasChanged(existing, rosterEvent))
                    {
                        existing.LastSyncedAt = DateTime.UtcNow;
                        existing.Status = rosterEvent.Status;
                        existing.Description = rosterEvent.Description;
                        existing.Destination = rosterEvent.Destination;
                        existing.EndTime = rosterEvent.EndTime;
                        existing.FlightNumber = rosterEvent.FlightNumber;
                        existing.Origin = rosterEvent.Origin;
                        existing.StartTime = rosterEvent.StartTime;
                        existing.RosterEventId = rosterEvent.Id;
                        updated++;
                    }
                }
                else
                {
                    var newEvent = new SyncedEvent
                    {
                        SyncConfig = config,
                        LastSyncedAt = DateTime.UtcNow,
                        GoogleEventId = "TEMP",
                        Status = rosterEvent.Status,
                        Type = rosterEvent.Type,
                        Description = rosterEvent.Description,
                        Destination = rosterEvent.Destination,
                        EndTime = rosterEvent.EndTime,
                        FlightNumber = rosterEvent.FlightNumber,
                        Origin = rosterEvent.Origin,
                        StartTime = rosterEvent.StartTime,
                        RosterEventId = rosterEvent.Id
                    };
                    db.SyncedEvents.Add(newEvent);
                    added++;
                }
            }

            foreach (var (key, dbEvent) in dbByKey)
            {
                if (!rosterByKey.ContainsKey(key))
                {
                    db.SyncedEvents.Remove(dbEvent);
                }
            }

            log.Status = "Success";
            log.FinishedAt = DateTime.UtcNow;
            log.EventsAdded = added;
            log.EventsUpdated = updated;

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.FinishedAt = DateTime.UtcNow;
            log.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string GetNaturalKey(RosterEvent e) => e.Type.ToLowerInvariant() switch
    {
        "flight" => $"flight_{e.FlightNumber}_{e.StartTime:yyyyMMddHHmm}",
        "nightstop" => $"nightstop_{e.Origin}_{e.StartTime:yyyyMMddHHmm}",
        _ => $"{e.Type}_{e.StartTime:yyyyMMddHHmm}"
    };

    private static string GetNaturalKey(SyncedEvent e) => e.Type.ToLowerInvariant() switch
    {
        "flight" => $"flight_{e.FlightNumber}_{e.StartTime:yyyyMMddHHmm}",
        "nightstop" => $"nightstop_{e.Origin}_{e.StartTime:yyyyMMddHHmm}",
        _ => $"{e.Type}_{e.StartTime:yyyyMMddHHmm}"
    };

    private static bool HasChanged(SyncedEvent db, RosterEvent roster) =>
        db.StartTime != roster.StartTime ||
        db.EndTime != roster.EndTime ||
        db.Status != roster.Status ||
        db.FlightNumber != roster.FlightNumber ||
        db.Origin != roster.Origin ||
        db.Destination != roster.Destination ||
        db.Description != roster.Description;
}