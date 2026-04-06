using Microsoft.EntityFrameworkCore;
using RosterSync.Core.Internals.Google.Calendar;
using RosterSync.Model;
using RosterSync.Model.Entities;

namespace RosterSync.Core;

public class RosterSyncService(
    IDbContext db,
    IRosterScraper scraper,
    IGoogleCalendarService calendarService)
{
    public async Task SyncAsync(int configId, CancellationToken cancellationToken)
    {
        var config = await db.SyncConfigs.SingleAsync(c=>c.Id == configId, cancellationToken);
        var log = new SyncLog
        {
            SyncConfig = config,
            SyncConfigId = config.Id,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };
        db.SyncLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var rosterEvents = await scraper.ScrapeAsync(config.RosterUrl, cancellationToken);
            if (!rosterEvents.Any())
            {
                log.FinishedAt = DateTime.UtcNow;
                log.Status = "No events found";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var firstSentEvent = rosterEvents.Min(e => e.StartTime);

            var dbEvents = await db.SyncedEvents
                .Where(e => e.SyncConfigId == config.Id)
                .ToListAsync(cancellationToken);

            var rosterByKey = rosterEvents.ToDictionary(GetNaturalKey);
            var dbByKey = dbEvents.ToDictionary(GetNaturalKey);

            var added = 0;
            var updated = 0;
            var deleted = 0;

            // Neu oder geändert
            foreach (var (key, rosterEvent) in rosterByKey)
            {
                if (dbByKey.TryGetValue(key, out var existing))
                {
                    if (HasChanged(existing, rosterEvent))
                    {
                        MapToEntity(existing, rosterEvent);
                        existing.LastSyncedAt = DateTime.UtcNow;

                        await calendarService.UpdateEventAsync(
                            config.UserId, config, existing, cancellationToken);

                        updated++;
                    }
                }
                else
                {
                    var newEvent = new SyncedEvent
                    {
                        SyncConfig = config,
                        LastSyncedAt = DateTime.UtcNow,
                        GoogleEventId = string.Empty,
                        Type = rosterEvent.Type,
                        Status = rosterEvent.Status
                    };
                    MapToEntity(newEvent, rosterEvent);
                    db.SyncedEvents.Add(newEvent);

                    // Erst speichern, dann Google ID zurückschreiben
                    await db.SaveChangesAsync(cancellationToken);

                    var googleId = await calendarService.CreateEventAsync(
                        config.UserId, config, newEvent, cancellationToken);

                    newEvent.GoogleEventId = googleId;
                    added++;
                }
            }

            // Weggefallen
            foreach (var (key, dbEvent) in dbByKey)
            {
                if (!rosterByKey.ContainsKey(key))
                {
                    if (dbEvent.StartTime > firstSentEvent)
                    {
                        await calendarService.DeleteEventAsync(
                            config.UserId, config, dbEvent.GoogleEventId, cancellationToken);
                    }

                    db.SyncedEvents.Remove(dbEvent);
                    deleted++;
                }
            }

            log.Status = "Success";
            log.FinishedAt = DateTime.UtcNow;
            log.EventsAdded = added;
            log.EventsUpdated = updated;
            log.EventsDeleted = deleted;

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

    private static void MapToEntity(SyncedEvent entity, RosterEvent roster)
    {
        entity.RosterEventId = roster.Id;
        entity.Type = roster.Type;
        entity.FlightNumber = roster.FlightNumber;
        entity.Origin = roster.Origin;
        entity.Destination = roster.Destination;
        entity.StartTime = roster.StartTime;
        entity.EndTime = roster.EndTime;
        entity.Status = roster.Status;
        entity.Description = roster.Description;
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