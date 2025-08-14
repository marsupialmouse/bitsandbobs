using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Identity;

public static class UpdateUserDetailsEndpoint
{
    public sealed record UpdateUserDetailsRequest(
        string? DisplayName = null,
        string? FirstName = null,
        string? LastName = null
    );

    public static async Task<Results<Ok, ValidationProblem, NotFound>> UpdateUserDetails(
        ClaimsPrincipal claimsPrincipal,
        [FromBody] UpdateUserDetailsRequest request,
        [FromServices] UserManager<User> userManager
    )
    {
        if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            return TypedResults.NotFound();

        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null! : request.DisplayName.Trim();
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();

        await userManager.UpdateAsync(user);

        return TypedResults.Ok();
    }
}
