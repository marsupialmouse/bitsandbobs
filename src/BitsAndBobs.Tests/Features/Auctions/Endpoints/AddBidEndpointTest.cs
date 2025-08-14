using System.Net.Http.Json;
using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

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
    public async Task ShouldSaveUserBidToDatabase()
    {
        var bidder = SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        response.ShouldNotBeNull();
        var userBid = await GetUserAuctionBid(bidder, auction.Id);
        userBid.ShouldNotBeNull();
        userBid.LastBidDate.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        userBid.Amount.ShouldBe(150m);
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

    [Test]
    public async Task ShouldPublishEventWhenBidAccepted()
    {
        var userId = SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        response.ShouldNotBeNull();
        (await Messaging.Published.Any<BidAccepted>(x =>
             {
                 var message = x.Context.Message;
                 return message.BidId == $"bid#{response.Id}"
                        && message.AuctionId == auction.Id.Value
                        && message.UserId == userId.Value
                        && message.PreviousCurrentBidderUserId == null
                        && message.CurrentBidderUserId == userId.Value;
             }
         )).ShouldBeTrue();
    }

    [Test]
    public async Task ShouldPublishEventWithPreviousBidderIdWhenBidAccepted()
    {
        var userId = SetAuthenticatedClaimsPrincipal();
        var previousBidderId = UserId.Create();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, previousBidderId, 100m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 150m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        response.ShouldNotBeNull();
        (await Messaging.Published.Any<BidAccepted>(x =>
             {
                 var message = x.Context.Message;
                 return  message.UserId == userId.Value
                        && message.PreviousCurrentBidderUserId == previousBidderId.Value
                        && message.CurrentBidderUserId == userId.Value;
             }
         )).ShouldBeTrue();
    }

    [Test]
    public async Task ShouldPublishEventWithUnchangedCurrentBidderIdWhenLowerBidAccepted()
    {
        var userId = SetAuthenticatedClaimsPrincipal();
        var previousBidderId = UserId.Create();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, previousBidderId, 150m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 140m);

        var httpResponse = await HttpClient.PostAsJsonAsync($"/api/auctions/{auction.Id.FriendlyValue}/bids", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<AddBidEndpoint.AddBidResponse>();

        response.ShouldNotBeNull();
        (await Messaging.Published.Any<BidAccepted>(x =>
             {
                 var message = x.Context.Message;
                 return  message.UserId == userId.Value
                         && message.PreviousCurrentBidderUserId == previousBidderId.Value
                         && message.CurrentBidderUserId == previousBidderId.Value;
             }
         )).ShouldBeTrue();
    }

    [Test]
    public async Task ShouldNotPublishEventWhenBidNotAccepted()
    {
        SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(initialPrice: 100m);
        var request = new AddBidEndpoint.AddBidRequest(auction.Id.FriendlyValue, 50m);

        await HttpClient.PostAsJsonAsync($"/api/auctions/{request.AuctionId}/bids", request);

        (await Messaging.Published.Any<BidAccepted>()).ShouldBeFalse();
    }

    private static Task<Bid?> GetBidFromDb(AuctionId auctionId, string bidId) =>
        DynamoContext.LoadAsync<Bid>(auctionId.Value, bidId)!;

    private static Task<UserAuctionBid> GetUserAuctionBid(UserId userId, AuctionId auctionId) =>
        DynamoContext.LoadAsync<UserAuctionBid>(userId.Value, auctionId.Value)!;
}
