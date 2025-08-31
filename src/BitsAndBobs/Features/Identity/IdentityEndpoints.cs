using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public static class IdentityEndpoints
{
    /// <summary>
    /// Maps endpoints for identity-related functions.
    /// </summary>
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var identityEndpoints = endpoints.MapGroup("/identity");
        identityEndpoints.MapIdentityApi<User>();
        identityEndpoints.MapGet("/details", GetUserDetailsEndpoint.GetUserDetails).RequireAuthorization();
        identityEndpoints.MapPost("/details", UpdateUserDetailsEndpoint.UpdateUserDetails).RequireAuthorization();
        identityEndpoints.MapPost("/jwt", GetJwtTokenEndpoint.GetJwtToken).RequireAuthorization();
        identityEndpoints.MapPost(
            "/logout",
            async (SignInManager<User> signInManager) => { await signInManager.SignOutAsync().ConfigureAwait(false); }
        );
    }
}
