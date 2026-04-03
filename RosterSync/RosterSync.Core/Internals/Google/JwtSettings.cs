namespace RosterSync.Core.Internals.Google;

public class JwtSettings
{
    public required string Secret { get; set; } 
    public int ExpiryDays { get; set; }
}