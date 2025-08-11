using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions;

public static class UserAuctionsEndpoints
{
    public sealed record GetUserAuctionsResponse([property: Required] IReadOnlyList<UserAuction> Auctions);

    public sealed record UserAuction(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] string ImageHref,
        [property: Required] decimal CurrentPrice,
        [property: Required] DateTimeOffset EndDate,
        [property: Required] bool IsOpen,
        [property: Required] bool IsClosed,
        [property: Required] bool IsCancelled,
        DateTimeOffset? CancelledDate
    )
    {
        [Required] public bool IsUserCurrentBidder { get; init; }
        public decimal? UserMaximumBid { get; init; }
    }

    public static async Task<Ok<GetUserAuctionsResponse>> GetSellerAuctions(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AuctionService auctionService,
        [FromServices] IOptions<AwsResourceOptions> options
    )
    {
        var auctions = await auctionService.GetUserAuctions(claimsPrincipal.GetUserId());
        var auctionResponses = auctions
                               .OrderByDescending(a => a.EndDate)
                               .Select(auction => new UserAuction(
                                           Id: auction.Id.FriendlyValue,
                                           Name: auction.Name,
                                           ImageHref: options.Value.GetAuctionImageHref(auction.Image),
                                           CurrentPrice: auction.CurrentPrice,
                                           EndDate: auction.EndDate,
                                           IsOpen: auction.IsOpen,
                                           IsClosed: auction.IsClosed,
                                           IsCancelled: auction.IsCancelled,
                                           CancelledDate: auction.CancelledDate
                                       )
                               )
                               .ToList();

        return TypedResults.Ok(new GetUserAuctionsResponse(auctionResponses));
    }
}
