namespace RosterSync.Api.Endpoints.Authentication.Internals;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl();
    Task<string> HandleCallbackAsync(string code, CancellationToken cancellationToken);
}