using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Identity;

public static class IdentityEndpoints
{
    [method: JsonConstructor]
    public sealed record DetailsResponse(
        [property: Required] string EmailAddress,
        [property: Required] string DisplayName,
        string? FirstName,
        string? LastName
    )
    {
        public DetailsResponse(User user) : this(user.EmailAddress, user.DisplayName, user.FirstName, user.LastName)
        {
        }
    }

    public sealed record DetailsRequest(
        string? DisplayName = null,
        string? FirstName = null,
        string? LastName = null
    );

    /// <summary>
    /// Maps endpoints for retrieving emails (since we don't want to mess around with actually sending emails).
    /// </summary>
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var identityEndpoints = endpoints.MapGroup("/identity");
        identityEndpoints.MapIdentityApi<User>();

        identityEndpoints.MapPost(
            "/logout",
            async (SignInManager<User> signInManager) => { await signInManager.SignOutAsync().ConfigureAwait(false); }
        );

        identityEndpoints.MapGet(
            "/details",
            async Task<Results<Ok<DetailsResponse>, ValidationProblem, NotFound>> (
                ClaimsPrincipal claimsPrincipal,
                [FromServices] UserManager<User> userManager
            ) =>
            {
                if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
                    return TypedResults.NotFound();

                return TypedResults.Ok(new DetailsResponse(user));
            }
        ).RequireAuthorization();

        identityEndpoints.MapPost(
            "/details",
            async Task<Results<Ok<DetailsResponse>, ValidationProblem, NotFound>> (
                ClaimsPrincipal claimsPrincipal,
                [FromBody] DetailsRequest request,
                [FromServices] UserManager<User> userManager
            ) =>
            {
                if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
                    return TypedResults.NotFound();

                user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null! : request.DisplayName.Trim();
                user.FirstName = request.FirstName?.Trim();
                user.LastName = request.LastName?.Trim();

                await userManager.UpdateAsync(user);

                return TypedResults.Ok(new DetailsResponse(user));
            }
        ).RequireAuthorization();
    }
}
