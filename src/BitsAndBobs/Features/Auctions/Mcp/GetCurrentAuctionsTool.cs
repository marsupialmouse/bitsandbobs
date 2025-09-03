using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BitsAndBobs.Features.Auctions.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetCurrentAuctionsTool
{
    public sealed record GetCurrentAuctionsResponse([property: Required] IReadOnlyList<CurrentAuction> Auctions);

    public sealed record CurrentAuction(
        [property: Required] string Id,
        [property: Required] string Name,
        [property: Required] string Description,
        [property: Required] decimal CurrentPrice,
        [property: Required] int NumberOfBids,
        [property: Required] DateTimeOffset EndDate,
        [property: Required] string SellerDisplayName
    );

    [McpServerTool(ReadOnly = true, UseStructuredContent = true), Description("Gets a list of current auctions")]
    public static async Task<GetCurrentAuctionsResponse> GetCurrentAuctions([FromServices] AuctionService auctionService)
    {
        using var diagnostics = new GetAuctionsDiagnostics();

        try
        {
            var auctions = await auctionService.GetActiveAuctions();

            var auctionResponses = auctions
                                   .OrderBy(a => a.EndDate)
                                   .Select(auction => new CurrentAuction(
                                               Id: auction.Id.FriendlyValue,
                                               Name: auction.Name,
                                               Description: auction.Description,
                                               CurrentPrice: auction.CurrentPrice,
                                               NumberOfBids: auction.NumberOfBids,
                                               EndDate: auction.EndDate,
                                               SellerDisplayName: auction.SellerDisplayName
                                           )
                                   );

            return new GetCurrentAuctionsResponse(auctionResponses.ToList());
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }
}
