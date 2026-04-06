using RosterSync.Core.Dtos;

namespace RosterSync.Api.Endpoints.Calendars;

public static class EndpointExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddCalendarEndpoints()
        {
            builder.MapGet("/api/calendars", CalendarEndpoints.GetUserCalendars())
                .Produces<IReadOnlyCollection<CalendarDto>>()
                .WithName("GetCalendars")
                .RequireAuthorization();

            return builder;
        }
    }
}