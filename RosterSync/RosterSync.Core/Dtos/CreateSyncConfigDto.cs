namespace RosterSync.Core.Dtos;

public record CreateSyncConfigDto(
    string GoogleCalendarId,
    string CalendarName,
    string RosterUrl
);