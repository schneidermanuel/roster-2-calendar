using RosterSync.Core.Dtos;

namespace RosterSync.Api.Endpoints.SyncConfigs;

public static class EndpointExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddSyncConfigEndpoints()
        {
            builder.MapGet("/api/syncs", SyncConfigEndpoints.GetSyncs())
                .Produces<IReadOnlyCollection<SyncConfigDto>>()
                .WithName("GetSyncs")
                .RequireAuthorization();
            builder.MapDelete("/api/syncs/{id:int}", SyncConfigEndpoints.DeleteSync())
                .Produces(StatusCodes.Status204NoContent)
                .WithName("DeleteSync")
                .RequireAuthorization();
            builder.MapPost("/api/syncs/", SyncConfigEndpoints.Create())
                .Produces<SyncConfigDto>(StatusCodes.Status201Created)
                .WithName("CreateSync")
                .RequireAuthorization();
            builder.MapPost("/api/syncs/{id:int}/sync", SyncConfigEndpoints.RunSync())
                .Produces(StatusCodes.Status202Accepted)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RunSync")
                .RequireAuthorization();

            return builder;
        }
    }
}