namespace RosterSync.Core.Internals.Google;

public interface ITokenRefreshService
{
    Task<string> GetValidAccessTokenAsync(Guid userId, CancellationToken cancellationToken);
}