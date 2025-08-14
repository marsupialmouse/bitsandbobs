using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Identity;

public static class GetUserDetailsEndpoint
{
    [method: JsonConstructor]
    public sealed record GetUserDetailsResponse(
        [property: Required] string EmailAddress,
        [property: Required] string DisplayName,
        string? FirstName,
        string? LastName
    )
    {
        public GetUserDetailsResponse(User user) : this(user.EmailAddress, user.DisplayName, user.FirstName, user.LastName)
        {
        }
    }

    public static async Task<Results<Ok<GetUserDetailsResponse>, ValidationProblem, NotFound>> GetUserDetails(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] UserManager<User> userManager
    )
    {
        if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            return TypedResults.NotFound();

        return TypedResults.Ok(new GetUserDetailsResponse(user));
    }
}
