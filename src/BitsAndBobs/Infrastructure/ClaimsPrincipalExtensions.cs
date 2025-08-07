using System.Security.Claims;

namespace BitsAndBobs.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the ID of an authenticated user
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
