using System.ComponentModel;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetAuctionTool
{
    [McpServerTool, Description("Gets the details of an auction by auction ID")]
    public static async Task<object> GetAuction(
        string id,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService,
        [FromServices] UserStore userStore
    )
    {
        if (!AuctionId.TryParse(id, out var auctionId))
        {
            GetAuctionDiagnostics.InvalidId(id);
            return new ErrorContent("Invalid ID");
        }

        using var diagnostics = new GetAuctionDiagnostics(auctionId);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(auctionId);

            if (auction is null)
            {
                diagnostics.NotFound();
                return new ErrorContent("Auction not found");
            }

            var claimsPrincipal = httpContextAccessor.HttpContext?.User!;
            var isAuthenticated = claimsPrincipal.Identity?.IsAuthenticated ?? false;
            var userId = isAuthenticated ? claimsPrincipal.GetUserId() : UserId.Empty;
            var currentBidder = auction.CurrentBidderId;
            var isUserCurrentBidder = userId == currentBidder;
            var bidderNames = await GetBidderDisplayNames(auction, userStore);

            diagnostics.Succeeded();

            return new
            {
                Id = auction.Id.FriendlyValue,
                auction.Name,
                auction.Description,
                auction.SellerDisplayName,
                auction.IsOpen,
                auction.IsClosed,
                auction.IsCancelled,
                IsUserSeller = userId == auction.SellerId,
                IsUserCurrentBidder = isUserCurrentBidder,
                CurrentBidderDisplayName = bidderNames.CurrentBidderName,
                auction.InitialPrice,
                auction.CurrentPrice,
                MinimumBid =
                    isUserCurrentBidder
                        ? Math.Max(auction.MinimumBid, auction.CurrentBid!.Amount + 0.01m)
                        : auction.MinimumBid,
                auction.NumberOfBids,
                auction.EndDate,
                auction.CancelledDate,
                Bids = auction.Bids.Select(bid => Bid(bid, auction, userId, bidderNames))
            };
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

    private static object Bid(Bid bid, Auction auction, UserId userId, BidderDisplayNames names)
    {
        var isUserBid = bid.BidderId == userId;

        return new
        {
            Id = bid.BidId[4..],
            Amount = bid.Amount > auction.CurrentPrice && !isUserBid ? auction.CurrentPrice : bid.Amount,
            BidderDisplayName = names.Get(bid.BidderId),
            bid.BidDate,
            IsUserBid = isUserBid,
            IsCurrentBid = bid.BidId == auction.CurrentBidId,
        };
    }

    private sealed class BidderDisplayNames(Auction? auction, Dictionary<UserId, string> names)
    {
        public static readonly BidderDisplayNames Empty = new(null, new Dictionary<UserId, string>());

        public string? CurrentBidderName =>
            auction?.CurrentBidderId != null ? Get(auction!.CurrentBidderId.Value) : null;
        public string Get(UserId id) => names.GetValueOrDefault(id, "Mr Mystery");
    }
}
