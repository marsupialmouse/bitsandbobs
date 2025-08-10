using System.Net;
using System.Security.Claims;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BitsAndBobs.Features.Auctions;

public class CancelAuctionEndpoint
{
    private static readonly ProblemHttpResult InvalidState = TypedResults.Problem(
        statusCode: (int)HttpStatusCode.BadRequest,
        title: "InvalidState"
    );

    public static async Task<Results<Ok, ProblemHttpResult, NotFound>> CancelAuction(
        AuctionId auctionId,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AuctionService auctionService
    )
    {
        var userId = claimsPrincipal.GetUserId();

        using var diagnostics = new CancelAuctionDiagnostics(auctionId, userId);

        try
        {
            var auction = await auctionService.GetAuction(auctionId);

            if (auction is null)
            {
                diagnostics.AuctionNotFound();
                return TypedResults.NotFound();
            }

            await auctionService.CancelAuction(auction, userId);

            diagnostics.Cancelled();

            return TypedResults.Ok();
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
        catch (InvalidOperationException e)
        {
            diagnostics.Failed(e);
            return TypedResults.Problem(statusCode: (int)HttpStatusCode.BadRequest);
        }
        catch (Exception e)
        {
            diagnostics.Failed(e);
            throw;
        }
    }
}
