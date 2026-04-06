namespace RosterSync.Core.Internals.Google;

public class AuthSettings
{
    public required GoogleSettings Google { get; set; }
    public required JwtSettings Jwt { get; set; }
    public required string FrontendUrl { get; set; }
}