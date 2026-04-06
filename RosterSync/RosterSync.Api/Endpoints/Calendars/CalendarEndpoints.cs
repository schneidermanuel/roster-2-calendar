using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RosterSync.Core.Internals.Google.Calendar;

namespace RosterSync.Api.Endpoints.Calendars;

public static class CalendarEndpoints
{
    public static Delegate GetUserCalendars() => async (CancellationToken cancellationToken, ClaimsPrincipal user,
        [FromServices] IGoogleCalendarService service) =>
    {
        var userId = user.GetUserId();
        var calendars = await service.GetOwnedCalendarsAsync(userId, cancellationToken);
        return Results.Ok(calendars);
    };
}