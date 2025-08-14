using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BitsAndBobs.Contracts;
using BitsAndBobs.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Identity;

public static class UpdateUserDetailsEndpoint
{
    public sealed record UpdateUserDetailsRequest(
        [property: Required] string DisplayName,
        string? FirstName = null,
        string? LastName = null
    );

    public static async Task<Results<Ok, ValidationProblem, NotFound>> UpdateUserDetails(
        ClaimsPrincipal claimsPrincipal,
        [FromBody] UpdateUserDetailsRequest request,
        [FromServices] IValidator<UpdateUserDetailsRequest> validator,
        [FromServices] UserManager<User> userManager,
        [FromServices] RecklessPublishEndpoint publishEndpoint
    )
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            return TypedResults.NotFound();

        var previousDisplayName = user.DisplayName;
        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null! : request.DisplayName.Trim();
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();

        await userManager.UpdateAsync(user);

        if (user.DisplayName != previousDisplayName)
        {
            await publishEndpoint.PublishRecklessly(
                new UserDisplayNameChanged(
                    UserId: user.Id.Value,
                    OldDisplayName: previousDisplayName,
                    NewDisplayName: user.DisplayName
                )
            );
        }

        return TypedResults.Ok();
    }

    public class UpdateUserDetailsValidator : AbstractValidator<UpdateUserDetailsRequest>
    {
        public UpdateUserDetailsValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .WithMessage("Display name is required");
        }
    }
}
