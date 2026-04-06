using RosterSync.Core.Dtos;

namespace RosterSync.Core.Internals.Google.Calendar;

public interface IGoogleCalendarService
{
    Task<IReadOnlyCollection<CalendarDto>> GetOwnedCalendarsAsync(Guid userId, CancellationToken cancellationToken);
}