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
        var googleEvent = MapToGoogleEvent(e);
        var created = await service.Events.Insert(googleEvent, config.GoogleCalendarId).ExecuteAsync(cancellationToken);
        return created.Id;
    }

    public async Task UpdateEventAsync(Guid userId, Model.Entities.SyncConfig config, SyncedEvent e,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        var googleEvent = MapToGoogleEvent(e);
        await service.Events.Update(googleEvent, config.GoogleCalendarId, e.GoogleEventId)
            .ExecuteAsync(cancellationToken);
    }

    public async Task DeleteEventAsync(Guid userId, Model.Entities.SyncConfig config, string googleEventId,
        CancellationToken cancellationToken)
    {
        var service = await CreateServiceAsync(userId, cancellationToken);
        await service.Events.Delete(config.GoogleCalendarId, googleEventId).ExecuteAsync(cancellationToken);
    }

    private static string? GetColor(SyncedEvent e)
    {
        if (e.Type == "off")
        {
            return "8";
        }

        if (e.Type == "duty" || e.Type == "standby" || e.Type == "reserve")
        {
            return "7";
        }

        return null;
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
                    UseDefault = false
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
                UseDefault = false
            },
            Start = new EventDateTime { DateTimeDateTimeOffset = e.StartTime, TimeZone = "UTC" },
            End = new EventDateTime { DateTimeDateTimeOffset = e.EndTime, TimeZone = "UTC" },
            ColorId = GetColor(e)
        };
    }

    private static string GetTitle(SyncedEvent e) => e.Type.ToLowerInvariant() switch
    {
        "flight" => $"{e.FlightNumber} {e.Origin}→{e.Destination}",
        "nightstop" => $"Nightstop {e.Origin}",
        "off" => "OFF",
        _ => e.Description ?? e.Type
    };
}