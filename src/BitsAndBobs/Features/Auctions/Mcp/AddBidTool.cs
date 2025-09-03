using System.ComponentModel;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public static class AddBidTool
{
    [McpServerTool, Description("Places a bid on an auction")]
    public static async Task<string> AddBid(
        string auctionId,
        decimal bidAmount,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AuctionService auctionService)
    {
        if (!AuctionId.TryParse(auctionId, out var id))
            throw new McpException("Invalid auction ID", McpErrorCode.InvalidParams);

        var userId = httpContextAccessor.GetUserId();

        using var diagnostics = new BidDiagnostics(id, userId, bidAmount);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(id);

            if (auction is null)
            {
                diagnostics.AuctionNotFound();
                throw new McpException("Auction not found");
            }

            diagnostics.AddAuctionDetails(auction);

            if (auction.SellerId == userId)
            {
                diagnostics.Invalid();
                throw new McpException("You cannot bid on your own auction");
            }

            await auctionService.AddBid(auction, userId, bidAmount);

            diagnostics.Accepted();

            return $"Bid accepted. The new price is {auction.CurrentPrice:C}";
        }
        catch (McpException)
        {
            throw;
        }
        catch (InvalidAuctionStateException e)
        {
            diagnostics.Invalid();
            throw new McpException(e.Message);
        }
        catch (DynamoDbConcurrencyException)
        {
            diagnostics.Invalid();
            throw new McpException("Another user has placed a bid on this auction. Re-get the auction and try again.");
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }
}
