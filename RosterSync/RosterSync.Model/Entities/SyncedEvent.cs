using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RosterSync.Model.Entities;

public class SyncedEvent
{
    public long Id { get; set; }
    public int RosterEventId { get; set; }
    public int SyncConfigId { get; set; }
    public required string GoogleEventId { get; set; }
    public required string Type { get; set; }
    public string? FlightNumber { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public required string Status { get; set; }
    public string? Description { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public required SyncConfig SyncConfig { get; set; }
}
file class SyncedEventConfiguration : IEntityTypeConfiguration<SyncedEvent>
{
    public void Configure(EntityTypeBuilder<SyncedEvent> builder)
    {
        builder.HasKey(se => se.Id);
        builder.HasIndex(se => new { se.SyncConfigId, se.RosterEventId }).IsUnique();
        builder.Property(se => se.Type).HasMaxLength(50);
        builder.Property(se => se.FlightNumber).HasMaxLength(20);
        builder.Property(se => se.Origin).HasColumnType("CHAR(3)");
        builder.Property(se => se.Destination).HasColumnType("CHAR(3)");
        builder.Property(se => se.Status).HasMaxLength(50);
        builder.Property(se => se.GoogleEventId).HasMaxLength(255);
        builder.Property(se => se.Description).HasMaxLength(2048);
        builder.HasOne(se => se.SyncConfig)
               .WithMany(c => c.SyncedEvents)
               .HasForeignKey(se => se.SyncConfigId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
