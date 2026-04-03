using Microsoft.EntityFrameworkCore;
using RosterSync.Model.Entities;

namespace RosterSync.Model;
 public interface IDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<SyncConfig> SyncConfigs { get; }
    DbSet<SyncedEvent> SyncedEvents { get; }
    DbSet<SyncLog> SyncLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}