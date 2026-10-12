using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using RosterSync.Core.Dtos;
using RosterSync.Model.Entities;

namespace RosterSync.Core.Internals.Google.Calendar;

public class GoogleCalendarService(ITokenRefreshService tokenRefresh) : IGoogleCalendarService
{
    private async Task<CalendarService> CreateServiceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accessToken = await tokenRefresh.GetValidAccessTokenAsync(userId, cancellationToken);
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "RosterSync"
        });
    }

    public async Task<IReadOnlyCollection<CalendarDto>> GetOwnedCalendarsAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        var list = await service.CalendarList.List().ExecuteAsync(cancellationToken);

        return list.Items
            .Where(c => c.AccessRole == "owner")
            .Select(c => new CalendarDto(c.Id, c.Summary))
            .ToList()
            .AsReadOnly();
    }

    public async Task<string> CreateEventAsync(Guid userId, Model.Entities.SyncConfig config, SyncedEvent e,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        return await InsertAsync(service, config, e, MapToGoogleEvent(e), cancellationToken);
    }

    // Deterministic id makes the insert idempotent: if a previous sync created the event but
    // failed to persist the id, the retry hits 409 instead of creating a duplicate.
    // Google event ids allow lowercase a-v and 0-9 (5-1024 chars); hex qualifies.
    private static string GetDeterministicEventId(Model.Entities.SyncConfig config, SyncedEvent e) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes($"rostersync:{config.Id}:{e.Id}")))
            .ToLowerInvariant();

    private static async Task<string> InsertAsync(CalendarService service, Model.Entities.SyncConfig config,
        SyncedEvent e, Event googleEvent, CancellationToken cancellationToken)
    {
        googleEvent.Id = GetDeterministicEventId(config, e);
        try
        {
            var created = await service.Events.Insert(googleEvent, config.GoogleCalendarId)
                .ExecuteAsync(cancellationToken);
            return created.Id;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Conflict)
        {
            // Id already used: event exists, or was deleted (id stays reserved). Overwrite and
            // un-cancel so it reflects current data.
            googleEvent.Status = "confirmed";
            var restored = await service.Events.Update(googleEvent, config.GoogleCalendarId, googleEvent.Id)
                .ExecuteAsync(cancellationToken);
            return restored.Id;
        }
    }

    public async Task<string> UpdateEventAsync(Guid userId, Model.Entities.SyncConfig config, SyncedEvent e,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        var googleEvent = MapToGoogleEvent(e);
        try
        {
            var updated = await service.Events.Update(googleEvent, config.GoogleCalendarId, e.GoogleEventId)
                .ExecuteAsync(cancellationToken);
            // Cancelled (deleted) events can still be updated and stay cancelled - recreate.
            if (updated.Status != "cancelled")
                return e.GoogleEventId;
        }
        catch (GoogleApiException ex) when (IsGone(ex))
        {
            // deleted in Google, recreate below
        }

        return await InsertAsync(service, config, e, googleEvent, cancellationToken);
    }

    public async Task DeleteEventAsync(Guid userId, Model.Entities.SyncConfig config, string googleEventId,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        try
        {
            await service.Events.Delete(config.GoogleCalendarId, googleEventId).ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException ex) when (IsGone(ex))
        {
            // already deleted in Google
        }
    }

    private static bool IsGone(GoogleApiException ex) =>
        ex.HttpStatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;

    private static string? GetColor(SyncedEvent e)
    {
        if (IsE1Flight(e))
        {
            return "4";
        }

        if (e.Type == "off")
        {
            return "8";
        }

        if (e.Type == "duty" || e.Type == "standby" || e.Type == "reserve" || e.Type == "ac")
        {
            return "7";
        }

        return null;
    }

    private static bool IsE1Flight(SyncedEvent e)
    {
        return Regex.IsMatch(e.Description, "HBJV.");
    }

    private static Event MapToGoogleEvent(SyncedEvent e)
    {
        if (e.StartTime.AddDays(1).Equals(e.EndTime))
        {
            return new Event
            {
                Summary = GetTitle(e),
                Description = e.Description,
                Start = new EventDateTime { Date = e.StartTime.ToString("yyyy-MM-dd") },
                End = new EventDateTime { Date = e.EndTime.ToString("yyyy-MM-dd") },
                Reminders = new Event.RemindersData
                {
                    UseDefault = false,
                    Overrides = GetReminders(e)
                },
                ColorId = GetColor(e)
            };
        }

        return new Event
        {
            Summary = GetTitle(e),
            Description = e.Description,
            Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = GetReminders(e)
            },
            Start = new EventDateTime { DateTimeDateTimeOffset = e.StartTime, TimeZone = "UTC" },
            End = new EventDateTime { DateTimeDateTimeOffset = e.EndTime, TimeZone = "UTC" },
            ColorId = GetColor(e)
        };
    }

    private static IList<EventReminder> GetReminders(SyncedEvent e)
    {
        if (IsE1Flight(e))
        {
            return
            [
                new EventReminder
                {
                    Method = "popup",
                    Minutes = 24 * 60
                }
            ];
        }

        return [];
    }

    private static string GetTitle(SyncedEvent e) => e.Type.ToLowerInvariant() switch
    {
        "flight" => $"{e.FlightNumber} {e.Origin}→{e.Destination}",
        "nightstop" => $"Nightstop {e.Origin}",
        "off" => "OFF",
        _ => e.Description ?? e.Type
    };
}