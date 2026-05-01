using RosterSync.Core.Internals.Google;

namespace RosterSync.Core.Internals;

public class AuthSettings
{
    public required GoogleSettings Google { get; set; }
    public required JwtSettings Jwt { get; set; }
    public required string FrontendUrl { get; set; }
    public required WahaSettings Waha { get; set; }
}