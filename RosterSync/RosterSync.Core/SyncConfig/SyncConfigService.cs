using Microsoft.EntityFrameworkCore;
using RosterSync.Core.Dtos;
using RosterSync.Model;

namespace RosterSync.Core.SyncConfig;

public class SyncConfigService(IDbContext db) : ISyncConfigService
{
    public async Task<IReadOnlyList<SyncConfigDto>> GetAllAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await db.SyncConfigs
            .Where(c => c.UserId == userId && c.IsActive)
            .Select(c => new SyncConfigDto(
                c.Id,
                c.CalendarName,
                c.RosterUrl,
                c.IsActive,
                db.SyncLogs
                    .Where(l => l.SyncConfigId == c.Id && l.Status == "Success")
                    .OrderByDescending(l => l.FinishedAt)
                    .Select(l => l.FinishedAt)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, int syncConfigId, CancellationToken cancellationToken = default)
    {
        var config = await db.SyncConfigs
                         .FirstOrDefaultAsync(c => c.Id == syncConfigId && c.UserId == userId, cancellationToken)
                     ?? throw new KeyNotFoundException("Sync not found");

        config.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyncConfigDto> CreateAsync(Guid userId, CreateSyncConfigDto dto,
        CancellationToken cancellationToken = default)
    {
        var randomTime = TimeOnly.FromTimeSpan(
            TimeSpan.FromMinutes(Random.Shared.Next(0, 22 * 60)));
        var user = await db.Users.SingleOrDefaultAsync(u=>u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new ArgumentException("User not found");
        }

        var config = new Model.Entities.SyncConfig
        {
            GoogleCalendarId = dto.GoogleCalendarId,
            CalendarName = dto.CalendarName,
            RosterUrl = dto.RosterUrl,
            IsActive = true,
            DailyTriggerTime = randomTime,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        db.SyncConfigs.Add(config);
        await db.SaveChangesAsync(cancellationToken);

        return new SyncConfigDto(config.Id, config.CalendarName, config.RosterUrl, config.IsActive, null);
    }
}