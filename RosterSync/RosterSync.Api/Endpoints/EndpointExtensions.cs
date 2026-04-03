using RosterSync.Core;

namespace RosterSync.Api.Endpoints;

public static class EndpointExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddRosterEndpoints()
        {
            builder.MapGet("/test/scrape", Endpoints.TestScraper())
                .Produces<IReadOnlyList<RosterEvent>>();
            
            return builder;
        }
    }
}