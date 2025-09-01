using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Infrastructure;

public static class HttpContextAccessorExtensions
{
    /// <summary>
    /// Gets the ID of an authenticated user
    /// </summary>
    public static UserId GetUserId(this IHttpContextAccessor accessor) => accessor.HttpContext!.User.GetUserId();
}
