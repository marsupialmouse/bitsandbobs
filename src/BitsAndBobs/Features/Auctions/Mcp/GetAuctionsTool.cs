using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetAuctionsTool
{
    [McpServerTool, Description("Gets a list of current auctions")]
    public static async Task<object> GetAuctions([FromServices] AuctionService auctionService)
    {
        var auctions = await auctionService.GetActiveAuctions();

        var auctionResponses = auctions
        .OrderBy(a => a.EndDate)
        .Select(auction => new
            {
                Id = auction.Id.FriendlyValue,
                auction.Name,
                auction.Description,
                auction.CurrentPrice,
                auction.NumberOfBids,
                auction.EndDate,
                auction.SellerDisplayName
            }
        );

        return new { auctions = auctionResponses };
    }
}
