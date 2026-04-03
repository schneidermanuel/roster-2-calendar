using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RosterSync.Model.Entities;

public class UserToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime TokenExpiry { get; set; }

    public required User User { get; set; }
}

file class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.AccessToken).HasColumnType("TEXT").IsRequired();
        builder.Property(t => t.RefreshToken).HasColumnType("TEXT").IsRequired();
        builder.Property(t => t.TokenExpiry).IsRequired();
        builder.HasOne(t => t.User)
            .WithOne(u => u.Token)
            .HasForeignKey<UserToken>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}