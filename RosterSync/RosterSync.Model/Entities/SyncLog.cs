using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RosterSync.Model.Entities;

public class SyncLog
{
    public long Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public required string Status { get; set; }
    public int EventsAdded { get; set; }
    public int EventsUpdated { get; set; }
    public int EventsDeleted { get; set; }
    public string? ErrorMessage { get; set; }

    public required SyncConfig SyncConfig { get; set; }
    public int SyncConfigId { get; set; }
}
file class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Status).HasMaxLength(50);
        builder.Property(l => l.ErrorMessage).HasMaxLength(2048);
        builder.HasOne(l => l.SyncConfig)
               .WithMany(c => c.SyncLogs)
               .HasForeignKey(l => l.SyncConfigId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
