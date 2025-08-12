using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Auctions;

public static class CreateAuctionEndpoint
{
    public sealed record CreateAuctionRequest(
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] string ImageId,
        [property: Required] decimal InitialPrice,
        [property: Required] decimal BidIncrement,
        [property: Required] TimeSpan Period
    );

    public sealed record CreateAuctionResponse([property: Required] string Id);

    private static readonly IEnumerable<KeyValuePair<string, string[]>> ImageNotFound =
    [
        new(nameof(CreateAuctionRequest.ImageId), ["Image not found or not owned by current user"]),
    ];

    private static readonly IEnumerable<KeyValuePair<string, string[]>> ConcurrencyError =
    [
        new(nameof(CreateAuctionRequest.ImageId), ["Image has been modified by another process. Please try again."]),
    ];

    public static async Task<Results<Ok<CreateAuctionResponse>, ValidationProblem, NotFound>> CreateAuction(
        CreateAuctionRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IValidator<CreateAuctionRequest> validator,
        [FromServices] UserManager<User> userManager,
        [FromServices] AuctionService auctionService
    )
    {
        var imageId = AuctionImageId.Parse(request.ImageId);
        var userId = claimsPrincipal.GetUserId();

        using var diagnostics = new CreateAuctionDiagnostics(imageId, userId);

        try
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                diagnostics.Invalid();
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var seller = await userManager.GetUserAsync(claimsPrincipal);

            if (seller == null)
            {
                diagnostics.UserNotFound();
                return TypedResults.NotFound();
            }

            var auction = await auctionService.CreateAuction(
                              seller,
                              request.Name,
                              request.Description,
                              imageId,
                              request.InitialPrice,
                              request.BidIncrement,
                              request.Period
                          );

            diagnostics.Created(auction);

            return TypedResults.Ok(new CreateAuctionResponse(auction.Id.FriendlyValue));
        }
        catch (DynamoDbConcurrencyException e)
        {
            diagnostics.Failed(e);
            return TypedResults.ValidationProblem(ConcurrencyError);
        }
        catch (ImageNotFoundException)
        {
            diagnostics.ImageNotFound();
            return TypedResults.ValidationProblem(ImageNotFound);
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }

    public class CreateAuctionValidator : AbstractValidator<CreateAuctionRequest>
    {
        public CreateAuctionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(2000);

            RuleFor(x => x.ImageId)
                .NotEmpty();

            RuleFor(x => x.InitialPrice)
                .GreaterThan(0)
                .WithMessage("Initial price must be greater than zero");

            RuleFor(x => x.BidIncrement)
                .GreaterThanOrEqualTo(0.1m)
                .WithMessage("Bid increment must be at least 10 cents");

            RuleFor(x => x.Period)
                .GreaterThanOrEqualTo(TimeSpan.FromMinutes(10))
                .LessThanOrEqualTo(TimeSpan.FromDays(5))
                .WithMessage("Auction period must be at least 10 minutes and no more than 5 days");
        }
    }
}
