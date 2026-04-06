using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using RosterSync.Core.Dtos;
using RosterSync.Model;

namespace RosterSync.Core.Internals.Google.Calendar;

public class GoogleCalendarService(IDbContext db) : IGoogleCalendarService
{
    public async Task<IReadOnlyCollection<CalendarDto>> GetOwnedCalendarsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var token = await db.UserTokens
                        .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken)
                    ?? throw new KeyNotFoundException("No token found for user");

        var credential = GoogleCredential.FromAccessToken(token.AccessToken);

        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "RosterSync"
        });

        var list = await service.CalendarList.List().ExecuteAsync(cancellationToken);

        return list.Items
            .Where(c => c.AccessRole == "owner")
            .Select(c => new CalendarDto(c.Id, c.Summary))
            .ToList()
            .AsReadOnly();
    }
}