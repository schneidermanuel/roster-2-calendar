namespace RosterSync.Core.Dtos;

public record SyncConfigDto(
    int Id,
    string CalendarName,
    string RosterUrl,
    bool IsActive,
    DateTime? LastSync
);