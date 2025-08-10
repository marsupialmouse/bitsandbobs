using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions;

public static class GetAuctionsEndpoint
{
    public sealed record SummaryAuctionResponse(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] string ImageHref,
        [property: Required] decimal CurrentPrice,
        [property: Required] int NumberOfBids,
        [property: Required] DateTimeOffset EndDate,
        [property: Required] string SellerDisplayName
    );

    public sealed record GetAuctionsResponse([property: Required] List<SummaryAuctionResponse> Auctions);

    public static async Task<Ok<GetAuctionsResponse>> GetAuctions(
        [FromServices] IDynamoDBContext dynamo,
        [FromServices] IOptions<AwsResourceOptions> options
    )
    {
        var search = dynamo.QueryAsync<Auction>(
            AuctionStatus.Open,
            QueryOperator.GreaterThan,
            [DateTimeOffset.Now.UtcTicks],
            new QueryConfig { IndexName = "AuctionsByStatus" }
        );
        var auctions = await search.GetRemainingAsync();

        var auctionResponses = auctions
            .OrderBy(a => a.EndDate)
            .Select(auction => new SummaryAuctionResponse(
                auction.Id.FriendlyValue,
                auction.Name,
                auction.Description,
                options.Value.GetAuctionImageHref(auction.Image),
                auction.CurrentPrice,
                auction.NumberOfBids,
                auction.EndDate,
                auction.SellerDisplayName
            ))
            .ToList();

        return TypedResults.Ok(new GetAuctionsResponse(auctionResponses));
    }
}
