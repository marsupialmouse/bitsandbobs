using System.Security.Claims;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the ID of an authenticated user
    /// </summary>
    public static UserId GetUserId(this ClaimsPrincipal principal) =>
        UserId.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
