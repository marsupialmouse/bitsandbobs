namespace BitsAndBobs.Features.Auctions;

public static class AuctionEndpoints
{
    /// <summary>
    /// Maps endpoints for auctions
    /// </summary>
    public static void MapAuctionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auctions");

        group.MapGet("/", GetAuctionsEndpoint.GetAuctions);
        group.MapPost("/", CreateAuctionEndpoint.CreateAuction).RequireAuthorization();
        group.MapPost("/images", UploadImageEndpoint.UploadImage).RequireAuthorization().DisableAntiforgery();
    }
}
