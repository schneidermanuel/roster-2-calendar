namespace RosterSync.Api.Endpoints.Authentication;

public static class EndpointExtensions
{

    extension(IEndpointRouteBuilder builder)
    {
        public IEndpointRouteBuilder AddAuthenticationEndpoints()
        {
            builder.MapGet("/auth/gogle/login", AuthenticationEndpoints.GoogleAuthorize())
                .Produces(StatusCodes.Status303SeeOther)
                .WithName("GoogleLogin")
                .WithDescription("Redirect to the google login screen");

            builder.MapGet("/auth/google/callback", AuthenticationEndpoints.CodeCallback())
                .Produces<string>()
                .WithName("GoogleCodeExchange")
                .WithDescription("Exchanges a code from google authentication for a jwt");
            return builder;
        }
    }
}