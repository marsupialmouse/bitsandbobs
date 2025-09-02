using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.S3.Model;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions.Endpoints;

public static class GetAuctionForRelistingEndpoint
{
    public sealed record GetAuctionForRelistingResponse(
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] string ImageId,
        [property: Required] string ImageHref,
        [property: Required] decimal InitialPrice,
        [property: Required] decimal BidIncrement
    );

    /// <summary>
    /// Copies an auction image and returns the details of the image and auction for relisting. To actually relist the
    /// auction you need to call create with the auction and image details.
    /// </summary>
    public static async Task<Results<Ok<GetAuctionForRelistingResponse>, ProblemHttpResult, NotFound>> GetAuctionForRelisting(
        string auctionId,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AuctionService auctionService,
        [FromServices] IAmazonS3 s3,
        [FromServices] IDynamoDBContext dynamo,
        [FromServices] IOptions<AwsResourceOptions> options
    )
    {
        var userId = claimsPrincipal.GetUserId();

        if (!AuctionId.TryParse(auctionId, out var id))
            return TypedResults.NotFound();

        var auction = await auctionService.GetAuction(id);

        if (auction is null)
            return TypedResults.NotFound();

        if (userId != auction.SellerId)
            return TypedResults.Problem(statusCode: (int)HttpStatusCode.BadRequest);

        if (auction.IsOpen)
            return TypedResults.Problem(statusCode: (int)HttpStatusCode.BadRequest, title: "Cannot relist open auction");

        var imageExtension = auction.Image.Split('.').Last();
        var image = new AuctionImage($".{imageExtension}", userId);

        await dynamo.SaveItem(image);

        await s3.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = options.Value.AppBucketName,
                DestinationBucket = options.Value.AppBucketName,
                SourceKey = $"auctionimages/{auction.Image}",
                DestinationKey = $"auctionimages/{image.FileName}",
            }
        );

        var response = new GetAuctionForRelistingResponse(
            Name: auction.Name,
            Description: auction.Description,
            ImageId: image.Id.FriendlyValue,
            ImageHref: options.Value.GetAuctionImageHref(image.FileName),
            InitialPrice: auction.InitialPrice,
            BidIncrement: auction.BidIncrement
        );

        return TypedResults.Ok(response);

    }
}
