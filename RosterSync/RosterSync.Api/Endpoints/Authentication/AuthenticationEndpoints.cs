using Microsoft.AspNetCore.Mvc;
using RosterSync.Api.Endpoints.Authentication.Internals;

namespace RosterSync.Api.Endpoints.Authentication;

public static class AuthenticationEndpoints
{
    public static Delegate GoogleAuthorize() => ([FromServices] IGoogleAuthService service) =>
    Results.Redirect(service.GetAuthorizationUrl());

    public static Delegate CodeCallback() => async (CancellationToken cancellationToken,
            [FromServices] IGoogleAuthService auth, [FromQuery] string code) =>
        Results.Ok((object?)await auth.HandleCallbackAsync(code, cancellationToken));
}