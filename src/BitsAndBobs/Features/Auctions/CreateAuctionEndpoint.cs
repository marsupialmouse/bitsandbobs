using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Identity;
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
        [FromServices] IAmazonDynamoDB dynamoClient,
        [FromServices] IDynamoDBContext dynamoContext
    )
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var seller = await userManager.GetUserAsync(claimsPrincipal);

        if (seller == null)
            return TypedResults.NotFound();

        var image = await dynamoContext.LoadAsync<AuctionImage>(
                        AuctionImageId.Parse(request.ImageId),
                        AuctionImage.SortKey
                    );

        if (image == null || image.UserId != seller.Id)
            return TypedResults.ValidationProblem(ImageNotFound);

        var auction = new Auction(
            seller,
            request.Name,
            request.Description,
            image,
            request.InitialPrice,
            request.BidIncrement,
            request.Period
        );

        try
        {
            var items = new List<TransactWriteItem>
            {
                new() { Put = dynamoContext.CreateInsertPut(auction) },
                new() { Put = dynamoContext.CreateUpdatePut(image) },
            };

            await dynamoClient.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return TypedResults.ValidationProblem(ConcurrencyError);
        }

        return TypedResults.Ok(new CreateAuctionResponse(auction.Id.FriendlyValue));
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
                .GreaterThan(0.1m)
                .WithMessage("Bid increment must be greater than 10 cents");

            RuleFor(x => x.Period)
                .GreaterThan(TimeSpan.FromMinutes(10))
                .LessThanOrEqualTo(TimeSpan.FromDays(2))
                .WithMessage("Auction period must be at least 10 minutes and no more than 2 days");
        }
    }
}
