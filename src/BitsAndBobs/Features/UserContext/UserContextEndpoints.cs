using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BitsAndBobs.Features.UserContext;

public static  class UserContextEndpoints
{
    public sealed record UserContextResponse(
        [property: Required] DateTimeOffset LocalDate,
        [property: Required] bool IsAuthenticated,
        string? Username,
        string? EmailAddress
    );

    public static void MapUserContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/usercontext",
            (ClaimsPrincipal claimsPrincipal) => new UserContextResponse(
                DateTimeOffset.Now,
                claimsPrincipal.Identity?.IsAuthenticated ?? false,
                claimsPrincipal.Identity?.Name,
                claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            )
        );
    }
}
