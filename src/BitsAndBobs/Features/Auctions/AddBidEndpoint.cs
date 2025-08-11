using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Auctions;

public static class AddBidEndpoint
{
    public sealed record AddBidRequest([property: Required] string AuctionId, [property: Required] decimal Amount);

    public sealed record AddBidResponse([property: Required] string Id);

    private static readonly ProblemHttpResult InvalidState = TypedResults.Problem(
        statusCode: (int)HttpStatusCode.BadRequest,
        title: "InvalidState"
    );

    public static async Task<Results<Ok<AddBidResponse>, ProblemHttpResult, NotFound>> AddBid(
        AddBidRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AuctionService auctionService)
    {
        var auctionId = AuctionId.Parse(request.AuctionId);
        var userId = claimsPrincipal.GetUserId();

        using var diagnostics = new BidDiagnostics(auctionId, userId, request.Amount);

        try
        {
            var auction = await auctionService.GetAuctionWithBids(auctionId);

            if (auction is null)
            {
                diagnostics.AuctionNotFound();
                return TypedResults.NotFound();
            }

            diagnostics.AddAuctionDetails(auction);

            var bid = await auctionService.AddBid(auction, userId, request.Amount);

            diagnostics.Accepted();

            return TypedResults.Ok(new AddBidResponse(bid.BidId[4..]));
        }
        catch (InvalidAuctionStateException)
        {
            diagnostics.Invalid();
            return InvalidState;
        }
        catch (DynamoDbConcurrencyException)
        {
            diagnostics.Invalid();
            return InvalidState;
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }
}
