using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public static class UserAuctionsTools
{
    public sealed record GetUserAuctionsResponse(IReadOnlyList<UserAuction> Auctions);

    public sealed record UserAuction(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] decimal CurrentPrice,
        [property: Required] int NumberOfBids,
        [property: Required] DateTimeOffset EndDate,
        [property: Required] bool IsOpen,
        [property: Required] bool IsClosed,
        [property: Required] bool IsCancelled,
        DateTimeOffset? CancelledDate
    )
    {
        public static UserAuction Create(Auction auction) => new(
            Id: auction.Id.FriendlyValue,
            Name: auction.Name,
            CurrentPrice: auction.CurrentPrice,
            NumberOfBids: auction.NumberOfBids,
            EndDate: auction.EndDate,
            IsOpen: auction.IsOpen,
            IsClosed: auction.IsClosed,
            IsCancelled: auction.IsCancelled,
            CancelledDate: auction.CancelledDate
        );

        [Description("Indicates if the current user is the current bidder for this auction")]
        public bool IsUserCurrentBidder { get; init; }

        [Description("The current user's maximum bid for this auction, if any")]
        public decimal? UserMaximumBid { get; init; }
    }

    [McpServerTool(ReadOnly = true, UseStructuredContent = true), Description("Gets the user's auctions")]
    public static async Task<GetUserAuctionsResponse> GetSellerAuctions(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService
    )
    {
        var auctions = await auctionService.GetUserAuctions(httpContextAccessor.GetUserId());
        var auctionResponses = auctions.OrderByDescending(a => a.EndDate).Select(UserAuction.Create).ToList();

        return new GetUserAuctionsResponse(auctionResponses);
    }

    [McpServerTool(ReadOnly = true, UseStructuredContent = true), Description("Gets auctions the user has won")]
    public static async Task<GetUserAuctionsResponse> GetWonAuctions(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService
    )
    {
        var auctions = await auctionService.GetWonAuctions(httpContextAccessor.GetUserId());
        var auctionResponses = auctions
                               .OrderByDescending(a => a.EndDate)
                               .Select(auction => UserAuction.Create(auction) with { IsUserCurrentBidder = true })
                               .ToList();

        return new GetUserAuctionsResponse(auctionResponses);
    }

    [McpServerTool(ReadOnly = true, UseStructuredContent = true), Description("Gets auctions the user has participated in")]
    public static async Task<GetUserAuctionsResponse> GetParticipantAuctions(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService
    )
    {
        var auctions = await auctionService.GetUserAuctionParticipation(httpContextAccessor.GetUserId());
        var auctionResponses = auctions
                               .OrderByDescending(a => a.LastBidDate)
                               .Select(p => UserAuction.Create(p.Auction) with
                                   {
                                       IsUserCurrentBidder = p.Auction.CurrentBidderId == p.UserId,
                                       UserMaximumBid = p.MaximumBid,
                                   }
                               )
                               .ToList();

        return new GetUserAuctionsResponse(auctionResponses);
    }
}
