namespace BitsAndBobs.Features.Auctions;

public static class AuctionEndpoints
{
    /// <summary>
    /// Maps endpoints for auctions
    /// </summary>
    public static void MapAuctionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auctions");

        group.MapPost("/images", UploadImageEndpoint.UploadImage).RequireAuthorization().DisableAntiforgery();
    }
}
