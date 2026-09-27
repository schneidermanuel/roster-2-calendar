using RosterSync.Model.Entities;

namespace RosterSync.Core.Dtos;

public record CreateSyncConfigDto(
    string GoogleCalendarId,
    string CalendarName,
    string RosterUrl,
    LinkType LinkType = LinkType.Html,
    string? PhoneNumber = null
);
