using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RosterSync.Api.Endpoints.Authentication.Internals;
using RosterSync.Core.Internals.Google;

namespace RosterSync.Api.Endpoints.Authentication;

public static class AuthenticationEndpoints
{
    public static Delegate GoogleAuthorize() => ([FromServices] IGoogleAuthService service) =>
    Results.Redirect(service.GetAuthorizationUrl());

    public static Delegate CodeCallback() => async (CancellationToken cancellationToken,
            [FromServices] IGoogleAuthService auth, [FromQuery] string code, IOptions<AuthSettings> settings) =>
    {
        var token = await auth.HandleCallbackAsync(code, cancellationToken);
        var frontendUrl = settings.Value.FrontendUrl;
        return Results.Redirect($"{frontendUrl}/login?token={token}");
    };
}