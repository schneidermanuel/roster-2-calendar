using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RosterSync.Core.Dtos;
using RosterSync.Core.SyncConfig;

namespace RosterSync.Api.Endpoints.SyncConfigs;

public static class SyncConfigEndpoints
{
    public static Delegate GetSyncs() => async (CancellationToken cancellationToken,
        [FromServices] ISyncConfigService service, ClaimsPrincipal user) =>
    {
            var userId = user.GetUserId();
            var syncs = await service.GetAllAsync(userId, cancellationToken);
            return Results.Ok(syncs);
    };
    public static Delegate DeleteSync() => async (CancellationToken cancellationToken,
        [FromServices] ISyncConfigService service, ClaimsPrincipal user, [FromRoute] int id) =>
    {
            var userId = user.GetUserId();
             await service.DeleteAsync(userId, id, cancellationToken);
            return Results.NoContent();
    };
    
    public static Delegate Create() => async (CancellationToken cancellationToken,
        [FromServices] ISyncConfigService service, ClaimsPrincipal user, [FromBody] CreateSyncConfigDto request) =>
    {
            var userId = user.GetUserId();
            var created = await service.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/syncs/{created.Id}", created);
    };
}