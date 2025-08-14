using System.Net.Http.Json;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

[TestFixture]
public class GetParticipantAuctionsEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn401WhenNotAuthenticated()
    {
        var response = await HttpClient.GetAsync("/api/auctions/participant");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturnEmptyCollectionWhenUserHasNoAuctions()
    {
        SetAuthenticatedClaimsPrincipal();

        var response =
            await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>(
                "/api/auctions/participant"
            );

        response.ShouldNotBeNull();
        response.Auctions.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReturnOnlyAuctionsUserParticipatedIn()
    {
        var user = await CreateAuthenticatedUser();
        var auction1 = await CreateAuction();
        var auction2 = await CreateAuction();
        var auction3 = await CreateAuction();
        var auction4 = await CreateAuction();
        await AddUserBid(auction1, user.Id, DateTimeOffset.Now.AddHours(-2), 27m);
        await AddUserBid(auction3, UserId.Create(), DateTimeOffset.Now.AddHours(-1), 13m);
        await AddUserBid(auction4, user.Id, DateTimeOffset.Now.AddMinutes(-54), 5m);

        var response =
            await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>(
                "/api/auctions/participant"
            );

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(2);
        response.Auctions.ShouldContain(a => a.Id == auction1.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == auction4.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldIncludeClosedAndCancelledAuctions()
    {
        var user = await CreateAuthenticatedUser();
        var openAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(1));
        var closedAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(-1), configure: a => a.Complete());
        var cancelledAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(1), configure: a => a.Cancel());
        await AddUserBid(openAuction, user.Id, DateTimeOffset.Now.AddHours(-2), 27m);
        await AddUserBid(closedAuction, user.Id, DateTimeOffset.Now.AddHours(-1), 13m);
        await AddUserBid(cancelledAuction, user.Id, DateTimeOffset.Now.AddMinutes(-54), 5m);

        var response =
            await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>(
                "/api/auctions/participant"
            );

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(3);
        response.Auctions.ShouldContain(a => a.Id == openAuction.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == closedAuction.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == cancelledAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldOrderAuctionsByLastBidDateDescending()
    {
        var user = await CreateAuthenticatedUser();
        var auction1 = await CreateAuction(name: "Lots of Panola");
        var auction2 = await CreateAuction(name: "Nudge from Hey Dad!");
        var auction3 = await CreateAuction(name: "Puffy Dog");
        await AddUserBid(auction1, user.Id, DateTimeOffset.Now.AddHours(-1), 27m);
        await AddUserBid(auction2, user.Id, DateTimeOffset.Now.AddHours(-2), 13m);
        await AddUserBid(auction3, user.Id, DateTimeOffset.Now.AddMinutes(-54), 5m);

        var response =
            await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>(
                "/api/auctions/participant"
            );

        response.ShouldNotBeNull();
        response.Auctions[0].Name.ShouldBe("Puffy Dog");
        response.Auctions[1].Name.ShouldBe("Lots of Panola");
        response.Auctions[2].Name.ShouldBe("Nudge from Hey Dad!");
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionDetails()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "");
        var user = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
                          seller: user,
                          name: "Test Auction",
                          description: "A test item",
                          initialPrice: 50.00m,
                          endDate: DateTimeOffset.Now.AddHours(1)
                      );
        await AddUserBid(auction, user.Id, DateTimeOffset.Now, 102.42m);

        var response =
            await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>(
                "/api/auctions/participant"
            );

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.First(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.Name.ShouldBe("Test Auction");
        auctionResponse.CurrentPrice.ShouldBe(50.00m);
        auctionResponse.ImageHref.ShouldBe($"/auctions/{auction.Image}");
        auctionResponse.EndDate.ShouldBe(auction.EndDate);
        auctionResponse.IsOpen.ShouldBeTrue();
        auctionResponse.IsClosed.ShouldBeFalse();
        auctionResponse.IsCancelled.ShouldBeFalse();
        auctionResponse.UserMaximumBid.ShouldBe(102.42m);
        auctionResponse.IsUserCurrentBidder.ShouldBeFalse();
    }

    private static Task AddUserBid(Auction auction, UserId bidder, DateTimeOffset bidDate, decimal amount)
    {
        var bid = new BidWithDate(auction.Id, bidder, amount);
        bid.WithDate(bidDate);

        return Testing.DynamoContext.SaveAsync(new UserAuctionBid(bid));
    }

    private class BidWithDate(AuctionId auction, UserId bidder, decimal amount) : Bid(auction, bidder, amount)
    {
        public void WithDate(DateTimeOffset date) => BidDate = date;
    }
}
