using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RosterSync.Model;
using RosterSync.Model.Entities;

namespace RosterSync.Core.Internals.Google;

public class TokenRefreshService(IDbContext db, IOptions<AuthSettings> settings) : ITokenRefreshService
{
    public async Task<string> GetValidAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = await db.UserTokens
                        .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken)
                    ?? throw new KeyNotFoundException($"No token found for user {userId}");

        if (token.TokenExpiry > DateTime.UtcNow.AddMinutes(5))
            return token.AccessToken;

        var newToken = await RefreshAsync(token, cancellationToken);
        return newToken.AccessToken;
    }

    private async Task<UserToken> RefreshAsync(UserToken token, CancellationToken cancellationToken)
    {
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = settings.Value.Google.ClientId,
                    ClientSecret = settings.Value.Google.ClientSecret
                },
                Scopes = ["openid", "email", "profile", CalendarService.Scope.Calendar]
            });

        var refreshed = await flow.RefreshTokenAsync(
            userId: token.UserId.ToString(),
            refreshToken: token.RefreshToken,
            taskCancellationToken: cancellationToken);

        token.AccessToken = refreshed.AccessToken;
        token.TokenExpiry = DateTime.UtcNow.AddSeconds(refreshed.ExpiresInSeconds ?? 3600);

        if (refreshed.RefreshToken is not null)
            token.RefreshToken = refreshed.RefreshToken;

        await db.SaveChangesAsync(cancellationToken);

        return token;
    }
}