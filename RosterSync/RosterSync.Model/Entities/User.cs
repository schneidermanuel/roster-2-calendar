using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RosterSync.Model.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string GoogleId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public required bool IsActive { get; set; }
    public UserToken? Token { get; set; }
    public ICollection<SyncConfig> SyncConfigs { get; set; } = [];
}

file class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();
        builder.HasIndex(u => u.GoogleId).IsUnique();
        builder.Property(u => u.GoogleId).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.DisplayName).HasMaxLength(255);

        builder.HasOne(u => u.Token)
            .WithOne(t => t.User)
            .HasForeignKey<UserToken>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SyncConfigs)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}