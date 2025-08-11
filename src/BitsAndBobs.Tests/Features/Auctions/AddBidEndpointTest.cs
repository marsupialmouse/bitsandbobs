using System.Net.Http.Json;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class AddBidEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenAddingBidWithoutAuthentication()
    {
        var request = new AddBidEndpoint.AddBidRequest(AuctionId.Create().FriendlyValue, 150m);

        var response = await HttpClient.PostAsJsonAsync($"/api/auctions/{request.AuctionId}/bids", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldGet404ResponseWhenAuctionDoesNotExist()
    {
        SetAuthenticatedClaimsPrincipal();
        var request = new AddBidEndpoint.AddBidRequest(AuctionId.Create().FriendlyValue, 150m);

        var response = await HttpClient.PostAsJsonAsync($"/api/auctions/{request.AuctionId}/bids", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldGet400ResponseWhenBidIsInvalid()
    {
        SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 50m);

        var response = await HttpClient.PostAsJsonAsync($"/api/auctions/{request.AuctionId}/bids", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldAddBidSuccessfully()
    {
        SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var response = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task ShouldSaveBidToDatabase()
    {
        var bidder = SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        response.ShouldNotBeNull();
        var bid = await GetBidFromDb(auction.Id, $"bid#{response.Id}");
        bid.ShouldNotBeNull();
        bid.BidderId.ShouldBe(bidder);
        bid.BidDate.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        bid.Amount.ShouldBe(150m);
    }

    [Test]
    public async Task ShouldSaveUpdatedAuctionToDatabase()
    {
        var bidder = SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        await AddBidToAuction(auction, UserId.Create(), 125m);

        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        var updatedAuction = await GetAuctionFromDb(auction.Id);
        updatedAuction.ShouldNotBeNull();
        updatedAuction.NumberOfBids.ShouldBe(2);
        updatedAuction.CurrentBidId.ShouldBe($"bid#{response!.Id}");
        updatedAuction.CurrentBidderId.ShouldBe(bidder);
        updatedAuction.CurrentPrice.ShouldBe(135m);
    }

    private static Task<Bid?> GetBidFromDb(AuctionId auctionId, string bidId) =>
        DynamoContext.LoadAsync<Bid>(auctionId.Value, bidId)!;
}
