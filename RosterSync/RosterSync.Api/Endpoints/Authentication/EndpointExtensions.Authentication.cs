namespace RosterSync.Api.Endpoints.Authentication;

public static class EndpointExtensions
{

    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddAuthenticationEndpoints()
        {
            builder.MapGet("/auth/google/login", AuthenticationEndpoints.GoogleAuthorize())
                .Produces<string>()
                .WithName("GoogleLogin")
                .WithDescription("Redirect to the google login screen");

            builder.MapGet("/auth/google/callback", AuthenticationEndpoints.CodeCallback())
                .Produces(StatusCodes.Status303SeeOther)
                .WithName("GoogleCodeExchange")
                .WithDescription("Exchanges a code from google authentication for a jwt");
            return builder;
        }
    }
}