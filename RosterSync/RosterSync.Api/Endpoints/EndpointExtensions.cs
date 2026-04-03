namespace RosterSync.Api.Endpoints;

public static class EndpointExtensions
{
    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddRosterEndpoints()
        {
            builder.MapGet("/test/scrape", Endpoints.TestScraper());
            return builder;
        }
    }
}