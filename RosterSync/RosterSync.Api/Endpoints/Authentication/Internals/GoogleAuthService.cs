using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RosterSync.Core.Internals.Google;
using RosterSync.Model.Entities;

namespace RosterSync.Api.Endpoints.Authentication.Internals;

public class GoogleAuthService(
    IDbContextFactory<RosterSyncDbContext> dbFactory,
    IJwtService jwtService,
    IOptions<AuthSettings> settings) : IGoogleAuthService
{
    public string GetAuthorizationUrl()
    {
        var flow = CreateFlow();
        return flow.CreateAuthorizationCodeRequest(
            settings.Value.Google.RedirectUri).Build().ToString();
    }

    public async Task<string> HandleCallbackAsync(string code, CancellationToken cancellationToken = default)
    {
        var flow = CreateFlow();

        // Code gegen Token tauschen
        TokenResponse tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId: "",
            code: code,
            redirectUri: settings.Value.Google.RedirectUri,
            taskCancellationToken: cancellationToken);

        // Google User Info laden
        var payload = await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // User anlegen oder updaten
        var user = await db.Users
            .Include(u => u.Token)
            .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(user);
        }
        else
        {
            user.Email = payload.Email;
            user.DisplayName = payload.Name;
        }

        // Token speichern / updaten
        if (user.Token is null)
        {
            user.Token = new UserToken
            {
                AccessToken = tokenResponse.AccessToken,
                UserId = user.Id,
                User = user,
                RefreshToken = tokenResponse.RefreshToken ?? user.Token?.RefreshToken ?? "",
                TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600)
            };
        }
        else
        {
            user.Token.AccessToken = tokenResponse.AccessToken;
            user.Token.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                user.Token.RefreshToken = tokenResponse.RefreshToken;
        }

        await db.SaveChangesAsync(cancellationToken);

        return jwtService.GenerateToken(user);
    }

    private GoogleAuthorizationCodeFlow CreateFlow() => new(
        new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = settings.Value.Google.ClientId,
                ClientSecret = settings.Value.Google.ClientSecret
            },
            Scopes = [CalendarService.Scope.Calendar]
        });
}