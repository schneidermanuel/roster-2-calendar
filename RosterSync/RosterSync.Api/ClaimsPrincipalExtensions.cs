using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RosterSync.Api;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetUserId()
        {
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("No sub claim found");
            return Guid.Parse(sub);
        }
    }
}