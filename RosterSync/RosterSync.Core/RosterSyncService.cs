using System.Text;
using Microsoft.EntityFrameworkCore;
using RosterSync.Core.Internals.Google.Calendar;
using RosterSync.Core.Waha.Internals;
using RosterSync.Model;
using RosterSync.Model.Entities;

namespace RosterSync.Core;

public class RosterSyncService(
    IDbContext db,
    IRosterScraper scraper,
    IGoogleCalendarService calendarService,
    WahaClient waha)
{
    public async Task SyncAsync(int configId, CancellationToken cancellationToken)
    {
        var config = await db.SyncConfigs.SingleAsync(c => c.Id == configId, cancellationToken);
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
            var whatsappNotification = new StringBuilder();
            var rosterEvents = await scraper.ScrapeAsync(config.RosterUrl, cancellationToken);
            if (!rosterEvents.Any())
            {
                log.FinishedAt = DateTime.UtcNow;
                log.Status = "No events found";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var firstSentEvent = rosterEvents.Min(e => e.StartTime.Date);

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
                        if (rosterEvent.StartTime.Date <= DateTime.Today.AddDays(1) &&
                            rosterEvent.StartTime.ToUniversalTime() > DateTime.UtcNow)
                        {
                            AppendEventChangedMessage(whatsappNotification, rosterEvent, existing);
                        }
                        
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
                    if (newEvent.StartTime.Date <= DateTime.Today.AddDays(1) &&
                        newEvent.StartTime.ToUniversalTime() > DateTime.UtcNow)
                    {
                        whatsappNotification.AppendLine($"New event at {newEvent.StartTime.ToString("dd.MM")} {newEvent.Description}");
                    }
                }
            }

            // Weggefallen
            foreach (var (key, dbEvent) in dbByKey)
            {
                if (!rosterByKey.ContainsKey(key))
                {
                    if (dbEvent.StartTime >= firstSentEvent)
                    {
                        await calendarService.DeleteEventAsync(
                            config.UserId, config, dbEvent.GoogleEventId, cancellationToken);
                    }

                    db.SyncedEvents.Remove(dbEvent);
                    if (dbEvent.StartTime.ToUniversalTime() > DateTime.UtcNow && dbEvent.StartTime.Date <= DateTime.Today.AddDays(1))
                    {
                        whatsappNotification.AppendLine($"Event at {dbEvent.StartTime.ToString("dd.MM")} {dbEvent.Description} has been cancelled");
                    }
                    deleted++;
                }
            }

            log.Status = "Success";
            log.FinishedAt = DateTime.UtcNow;
            log.EventsAdded = added;
            log.EventsUpdated = updated;
            log.EventsDeleted = deleted;

            await db.SaveChangesAsync(cancellationToken);
            var whatsapp = whatsappNotification.ToString();
            if (!string.IsNullOrWhiteSpace(whatsapp) && !string.IsNullOrEmpty(config.PhoneNumber))
            {
                await waha.SendMessage(config.PhoneNumber!, $"You have new duty changes. Please review now\n\n{whatsapp}",
                    cancellationToken);
            }
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

    private void AppendEventChangedMessage(StringBuilder whatsappNotification, RosterEvent rosterEvent,
        SyncedEvent existing)
    {
        if (rosterEvent.Type == "flight")
        {
            whatsappNotification.Append(
                $"Change at {rosterEvent.StartTime.ToString("dd.MM")} on Flight {rosterEvent.FlightNumber}: ");
            if (GetAircraft(existing) != rosterEvent.Aircraft)
            {
                whatsappNotification.Append($"Aircraft changed to {rosterEvent.Aircraft}.");
            }
            else if (rosterEvent.Description != existing.Description)
            {
                whatsappNotification.Append("Description changed.");
            }

            whatsappNotification.AppendLine();
            return;
        }

        whatsappNotification.AppendLine(
            $"Change at {rosterEvent.StartTime.ToString("dd.MM")} on {rosterEvent.Description}");
    }

    private string? GetAircraft(SyncedEvent flight)
    {
        var parts = flight.Description?.Split(", ") ?? [];
        if (parts.Length > 1)
        {
            return parts[1].Trim();
        }

        return null;
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
        "flight" => $"flight_{e.FlightNumber}_{e.StartTime:yyyyMMdd}",
        "nightstop" => $"nightstop_{e.Origin}_{e.StartTime:yyyyMMdd}",
        _ => $"{e.Type}_{e.StartTime:yyyyMMdd}"
    };

    private static string GetNaturalKey(SyncedEvent e) => e.Type.ToLowerInvariant() switch
    {
        "flight" => $"flight_{e.FlightNumber}_{e.StartTime:yyyyMMdd}",
        "nightstop" => $"nightstop_{e.Origin}_{e.StartTime:yyyyMMdd}",
        _ => $"{e.Type}_{e.StartTime:yyyyMMdd}"
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