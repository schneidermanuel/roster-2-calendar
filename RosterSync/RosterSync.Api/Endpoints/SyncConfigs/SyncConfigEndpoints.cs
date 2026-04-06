using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosterSync.Core.Dtos;
using RosterSync.Core.SyncConfig;
using RosterSync.Model;

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

    public static Delegate RunSync() => async (CancellationToken cancellationToken,
        [FromServices] WorkerQueue queue, IDbContext context, ClaimsPrincipal user, int id) =>
    {
        var config =
            await context.SyncConfigs.SingleOrDefaultAsync(
                x => x.Id == id && x.IsActive && x.UserId == user.GetUserId(), cancellationToken);
        if (config is null)
        {
            return Results.BadRequest();
        }

        queue.Enqueue(config.Id);
        return Results.Accepted();
    };
}