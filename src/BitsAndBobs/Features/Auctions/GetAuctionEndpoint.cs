using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions;

public static class GetAuctionEndpoint
{
    /// <summary>
    /// The full details of an auction.
    /// </summary>
    /// <param name="Id">The auction ID</param>
    /// <param name="Name">The name of the item</param>
    /// <param name="Description">A description of the item</param>
    /// <param name="ImageHref">The URL of the item image</param>
    /// <param name="SellerDisplayName">The seller's display name</param>
    /// <param name="IsOpen">Whether the auction is still open for bids</param>
    /// <param name="IsClosed">Whether the auction has finished</param>
    /// <param name="IsCancelled">Whether the auction is cancelled</param>
    /// <param name="IsUserSeller">Whether the current user is the seller</param>
    /// <param name="IsUserCurrentBidder">Whether the current user is the current high bidder</param>
    /// <param name="InitialPrice">The initial price of the lot</param>
    /// <param name="CurrentPrice">The current price</param>
    /// <param name="CurrentBidderDisplayName">The display name of the current high bidder</param>
    /// <param name="MinimumBid">The minimum bid required to take part in the auction</param>
    /// <param name="NumberOfBids">The nuber of bids.</param>
    /// <param name="Bids">A list of all bids. This is only returned for authenticated users.</param>
    /// <param name="EndDate">The end date of the auction</param>
    /// <param name="CancelledDate">The date the auction was cancelled, if it is cancelled</param>

    public sealed record GetAuctionResponse(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] string ImageHref,
        [property: Required] string SellerDisplayName,
        [property: Required] bool IsOpen,
        [property: Required] bool IsClosed,
        [property: Required] bool IsCancelled,
        [property: Required] bool IsUserSeller,
        [property: Required] bool IsUserCurrentBidder,
        [property: Required] decimal InitialPrice,
        [property: Required] decimal CurrentPrice,
        string? CurrentBidderDisplayName,
        [property: Required] decimal MinimumBid,
        [property: Required] int NumberOfBids,
        [property: Required] IReadOnlyList<BidDetails>? Bids,
        [property: Required] DateTimeOffset EndDate,
        DateTimeOffset? CancelledDate
    );

    /// <summary>
    /// Details of an individual bid in an auction
    /// </summary>
    /// <param name="Amount">
    /// The bid amount. If this is the current high bid the true amount will only be shown to the bidder (since it
    /// might be greater than the current price)
    /// </param>
    /// <param name="BidderDisplayName">The display name of the bidder</param>
    /// <param name="BidDate">The date the bid was placed</param>
    /// <param name="IsUserBid">Whether the bid belongs to the current user</param>
    /// <param name="IsCurrentBid">Whether this is the current bid</param>
    public sealed record BidDetails(
        [property: Required] string Id,
        [property: Required] decimal Amount,
        [property: Required] string BidderDisplayName,
        [property: Required] DateTimeOffset BidDate,
        [property: Required] bool IsUserBid,
        [property: Required] bool IsCurrentBid
    );

    public static async Task<Results<Ok<GetAuctionResponse>, NotFound>> GetAuction(
        AuctionId id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AuctionService auctionService,
        [FromServices] UserStore userStore,
        [FromServices] IOptions<AwsResourceOptions> options
    )
    {
        using var diagnostics = new GetAuctionDiagnostics(id);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(id);

            if (auction is null)
            {
                diagnostics.NotFound();
                return TypedResults.NotFound();
            }

            var isAuthenticated = claimsPrincipal.Identity?.IsAuthenticated ?? false;
            var userId = isAuthenticated ? claimsPrincipal.GetUserId() : UserId.Empty;
            var currentBidder = auction.CurrentBidderId;
            var isUserCurrentBidder = userId == currentBidder;
            var displayNames = await GetBidderDisplayNames(auction, isAuthenticated, userStore);

            var response = new GetAuctionResponse(
                Id: auction.Id.FriendlyValue,
                Name: auction.Name,
                Description: auction.Description,
                ImageHref: options.Value.GetAuctionImageHref(auction.Image),
                SellerDisplayName: auction.SellerDisplayName,
                IsOpen: auction.IsOpen,
                IsClosed: !auction.IsOpen && auction.Status != AuctionStatus.Cancelled,
                IsCancelled: auction.Status == AuctionStatus.Cancelled,
                IsUserSeller: userId == auction.SellerId,
                IsUserCurrentBidder: isUserCurrentBidder,
                InitialPrice: auction.InitialPrice,
                CurrentPrice: auction.CurrentPrice,
                CurrentBidderDisplayName: displayNames.CurrentBidderName,
                MinimumBid: isUserCurrentBidder
                                ? Math.Max(auction.MinimumBid, auction.CurrentBid!.Amount + 0.01m)
                                : auction.MinimumBid,
                NumberOfBids: auction.NumberOfBids,
                Bids: isAuthenticated ? auction.Bids.Select(b => Bid(b, auction, userId, displayNames)).ToList() : null,
                EndDate: auction.EndDate,
                CancelledDate: auction.CancelledDate
            );

            diagnostics.Succeeded();

            return TypedResults.Ok(response);
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }

    private static async Task<BidderDisplayNames> GetBidderDisplayNames(
        Auction auction,
        bool isUserAuthenticated,
        UserStore userStore
    )
    {
        if (auction.CurrentBidderId == null)
            return BidderDisplayNames.Empty;

        var users = new HashSet<UserId>(
            isUserAuthenticated ? auction.Bids.Select(x => x.BidderId) : [auction.CurrentBidderId.Value]
        );
        return new BidderDisplayNames(auction, await userStore.GetDisplayNames(users));
    }

    private static BidDetails Bid(Bid bid, Auction auction, UserId userId, BidderDisplayNames names)
    {
        var isUserBid = bid.BidderId == userId;

        return new BidDetails(
            Id: bid.BidId[4..],
            Amount: bid.Amount > auction.CurrentPrice && !isUserBid ? auction.CurrentPrice : bid.Amount,
            BidderDisplayName: names.Get(bid.BidderId),
            BidDate: bid.BidDate,
            IsUserBid: isUserBid,
            IsCurrentBid: bid.BidId == auction.CurrentBidId
        );
    }

    private sealed class BidderDisplayNames(Auction? auction, Dictionary<UserId, string> names)
    {
        public static readonly BidderDisplayNames Empty = new(null, new Dictionary<UserId, string>());

        public string? CurrentBidderName =>
            auction?.CurrentBidderId != null ? Get(auction!.CurrentBidderId.Value) : null;
        public string Get(UserId id) => names.GetValueOrDefault(id, "Mr Mystery");
    }
}
