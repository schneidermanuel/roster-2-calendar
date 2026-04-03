using Microsoft.EntityFrameworkCore;
using RosterSync.Model;
using RosterSync.Model.Entities;

namespace RosterSync.Api;

public class RosterSyncDbContext(DbContextOptions<RosterSyncDbContext> options)
    : DbContext(options), IDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<SyncConfig> SyncConfigs => Set<SyncConfig>();
    public DbSet<SyncedEvent> SyncedEvents => Set<SyncedEvent>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IDbContext).Assembly);
    }
}