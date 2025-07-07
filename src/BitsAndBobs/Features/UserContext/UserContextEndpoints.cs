using System.Security.Claims;

namespace BitsAndBobs.Features.UserContext;

public static  class UserContextEndpoints
{
    public sealed record UserContextResponse(bool IsAuthenticated, string? Username, string? EmailAddress);

    public static void MapUserContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/usercontext",
            (ClaimsPrincipal claimsPrincipal) => new UserContextResponse(
                claimsPrincipal.Identity?.IsAuthenticated ?? false,
                claimsPrincipal.Identity?.Name,
                claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            )
        );
    }
}
