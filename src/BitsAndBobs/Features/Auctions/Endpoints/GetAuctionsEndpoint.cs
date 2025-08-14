using System.ComponentModel.DataAnnotations;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions.Endpoints;

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
        [FromServices] AuctionService auctionService,
        [FromServices] IOptions<AwsResourceOptions> options
    )
    {
        using var diagnostics = new GetAuctionsDiagnostics();

        try
        {
            var auctions = await auctionService.GetActiveAuctions();

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
                                           )
                                   )
                                   .ToList();

            var response = new GetAuctionsResponse(auctionResponses);

            diagnostics.Succeeded();

            return TypedResults.Ok(response);
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }
}
