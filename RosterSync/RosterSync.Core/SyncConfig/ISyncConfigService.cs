using RosterSync.Core.Dtos;

namespace RosterSync.Core.SyncConfig;

public interface ISyncConfigService
{
    Task<IReadOnlyList<SyncConfigDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, int syncConfigId, CancellationToken cancellationToken);

    Task<SyncConfigDto> CreateAsync(Guid userId, CreateSyncConfigDto dto,
        CancellationToken cancellationToken);
}