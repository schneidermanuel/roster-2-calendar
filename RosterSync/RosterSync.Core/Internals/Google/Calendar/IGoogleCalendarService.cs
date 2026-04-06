using RosterSync.Core.Dtos;
using RosterSync.Model.Entities;

namespace RosterSync.Core.Internals.Google.Calendar;

public interface IGoogleCalendarService
{
    Task<IReadOnlyCollection<CalendarDto>> GetOwnedCalendarsAsync(Guid userId, CancellationToken cancellationToken);

    Task<string> CreateEventAsync(Guid userId, Model.Entities.SyncConfig config, SyncedEvent syncedEvent,
        CancellationToken cancellationToken);

    Task UpdateEventAsync(Guid userId, Model.Entities.SyncConfig config, SyncedEvent syncedEvent,
        CancellationToken cancellationToken);

    Task DeleteEventAsync(Guid userId, Model.Entities.SyncConfig config, string googleEventId,
        CancellationToken cancellationToken);
}