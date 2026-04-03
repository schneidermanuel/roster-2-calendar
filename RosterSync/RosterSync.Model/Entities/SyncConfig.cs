using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RosterSync.Model.Entities;

public class SyncConfig
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public required string RosterUrl { get; set; }
    public required string GoogleCalendarId { get; set; }
    public required string CalendarName { get; set; }
    public bool IsActive { get; set; } = true;
    public TimeOnly DailyTriggerTime { get; set; }
    public DateTime CreatedAt { get; set; }

    public required User User { get; set; }
    public ICollection<SyncedEvent> SyncedEvents { get; set; } = [];
    public ICollection<SyncLog> SyncLogs { get; set; } = [];
}

file class SyncConfigConfiguration : IEntityTypeConfiguration<SyncConfig>
{
    public void Configure(EntityTypeBuilder<SyncConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.RosterUrl).HasMaxLength(2048);
        builder.Property(c => c.GoogleCalendarId).HasMaxLength(500);
        builder.Property(c => c.CalendarName).HasMaxLength(255);
    }
}