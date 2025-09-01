using System.ComponentModel;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public static class AddBidTool
{
    [McpServerTool, Description("Places a bid on an auction")]
    public static async Task<AIContent> AddBid(
        string auctionId,
        decimal bidAmount,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService)
    {
        var id = AuctionId.Parse(auctionId);
        var userId = httpContextAccessor.GetUserId();

        using var diagnostics = new BidDiagnostics(id, userId, bidAmount);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(id);

            if (auction is null)
            {
                diagnostics.AuctionNotFound();
                return new ErrorContent("Auction not found");
            }

            diagnostics.AddAuctionDetails(auction);

            if (auction.SellerId == userId)
            {
                diagnostics.Invalid();
                return new ErrorContent("You cannot bid on your own auction");
            }

            await auctionService.AddBid(auction, userId, bidAmount);

            diagnostics.Accepted();

            return new TextContent($"Bid accepted. The new price is {auction.CurrentPrice:C}");
        }
        catch (InvalidAuctionStateException e)
        {
            diagnostics.Invalid();
            return new ErrorContent(e.Message);
        }
        catch (DynamoDbConcurrencyException)
        {
            diagnostics.Invalid();
            return new ErrorContent("Another user has placed a bid on this auction");
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            return new ErrorContent("Failed to add bid");
        }
    }
}
