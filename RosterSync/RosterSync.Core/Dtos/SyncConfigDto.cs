using RosterSync.Model.Entities;

namespace RosterSync.Core.Dtos;

public record SyncConfigDto(
    int Id,
    string CalendarName,
    string RosterUrl,
    LinkType LinkType,
    bool IsActive,
    DateTime? LastSync,
    string? PhoneNumber
);
