using RosterSync.Model.Entities;

namespace RosterSync.Api.Endpoints.Authentication.Internals;

public interface IJwtService
{
    string GenerateToken(User user);
}