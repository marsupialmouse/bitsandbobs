using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetAuctionTool
{
    public sealed record GetAuctionResponse(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] string SellerDisplayName,
        [property: Required] bool IsOpen,
        [property: Required] bool IsClosed,
        [property: Required] bool IsCancelled,
        [property: Required, Description("Whether the current user is the seller in this auction")] bool IsUserSeller,
        [property: Required, Description("Whether the current user is the currently winning the auction")] bool IsUserCurrentBidder,
        [property: Required] decimal InitialPrice,
        [property: Required] decimal CurrentPrice,
        string? CurrentBidderDisplayName,
        [property: Required, Description("The minimum acceptable next bid")] decimal MinimumBid,
        [property: Required] int NumberOfBids,
        IReadOnlyList<BidDetails>? Bids,
        [property: Required] DateTimeOffset EndDate,
        DateTimeOffset? CancelledDate);

    public sealed record BidDetails(
        [property: Required] string Id,
        [property: Required] decimal Amount,
        [property: Required] string BidderDisplayName,
        [property: Required] DateTimeOffset BidDate,
        [property: Required, Description("Whether this bid belongs to the current user")] bool IsUserBid,
        [property: Required, Description("Whether this bid is the current winning bid")] bool IsCurrentBid
    );

    [McpServerTool(ReadOnly = true, UseStructuredContent = true), Description("Gets the details of an auction by auction ID")]
    public static async Task<GetAuctionResponse> GetAuction(
        string id,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService,
        [FromServices] UserStore userStore
    )
    {
        if (!AuctionId.TryParse(id, out var auctionId))
        {
            GetAuctionDiagnostics.InvalidId(id);
            throw new McpException("Invalid auction ID");
        }

        using var diagnostics = new GetAuctionDiagnostics(auctionId);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(auctionId);

            if (auction is null)
            {
                diagnostics.NotFound();
                throw new McpException("Auction not found");
            }

            var claimsPrincipal = httpContextAccessor.HttpContext?.User!;
            var isAuthenticated = claimsPrincipal.Identity?.IsAuthenticated ?? false;
            var userId = isAuthenticated ? claimsPrincipal.GetUserId() : UserId.Empty;
            var currentBidder = auction.CurrentBidderId;
            var isUserCurrentBidder = userId == currentBidder;
            var bidderNames = await GetBidderDisplayNames(auction, userStore);

            diagnostics.Succeeded();

            return new GetAuctionResponse(
                Id: auction.Id.FriendlyValue,
                Name: auction.Name,
                Description: auction.Description,
                SellerDisplayName: auction.SellerDisplayName,
                IsOpen: auction.IsOpen,
                IsClosed: auction.IsClosed,
                IsCancelled: auction.IsCancelled,
                IsUserSeller: userId == auction.SellerId,
                IsUserCurrentBidder: isUserCurrentBidder,
                InitialPrice: auction.InitialPrice,
                CurrentPrice: auction.CurrentPrice,
                CurrentBidderDisplayName: bidderNames.CurrentBidderName,
                MinimumBid: isUserCurrentBidder
                                ? Math.Max(auction.MinimumBid, auction.CurrentBid!.Amount + 0.01m)
                                : auction.MinimumBid,
                NumberOfBids: auction.NumberOfBids,
                Bids: auction.Bids.Select(bid => Bid(bid, auction, userId, bidderNames)).ToList(),
                EndDate: auction.EndDate,
                CancelledDate: auction.CancelledDate
            );
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }

    private static async Task<BidderDisplayNames> GetBidderDisplayNames(Auction auction, UserStore userStore)
    {
        if (auction.CurrentBidderId == null)
            return BidderDisplayNames.Empty;

        var users = new HashSet<UserId>(auction.Bids.Select(x => x.BidderId));
        return new BidderDisplayNames(auction, await userStore.GetDisplayNames(users));
    }

    private static BidDetails Bid(Bid bid, Auction auction, UserId userId, BidderDisplayNames names)
    {
        var isUserBid = bid.BidderId == userId;

        return new BidDetails
        (
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
