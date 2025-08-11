using System.Net.Http.Json;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class GetSellerAuctionsEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn401WhenNotAuthenticated()
    {
        var response = await HttpClient.GetAsync("/api/auctions/seller");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturnEmptyCollectionWhenUserHasNoAuctions()
    {
        SetAuthenticatedClaimsPrincipal();

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/seller");

        response.ShouldNotBeNull();
        response.Auctions.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReturnOnlyUsersAuctions()
    {
        var user = await CreateAuthenticatedUser();
        var auction1 = await CreateAuction(seller: user, name: "First Auction");
        var auction2 = await CreateAuction(seller: user, name: "Second Auction");
        await CreateAuction(seller: new User(), name: "Other User Auction");

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/seller");

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(2);
        response.Auctions.ShouldContain(a => a.Id == auction1.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == auction2.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldIncludeClosedAndCancelledAuctions()
    {
        var user = await CreateAuthenticatedUser();
        var openAuction = await CreateAuction(seller: user, name: "Open Auction", endDate: DateTimeOffset.Now.AddHours(1));
        var closedAuction = await CreateAuction(seller: user, name: "Closed Auction", endDate: DateTimeOffset.Now.AddHours(-1));
        var cancelledAuction = await CreateAuction(seller: user, name: "Cancelled Auction", endDate: DateTimeOffset.Now.AddHours(1), configure: a => a.Cancel());

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/seller");

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(3);
        response.Auctions.ShouldContain(a => a.Id == openAuction.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == closedAuction.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == cancelledAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldOrderAuctionsByEndDateDescending()
    {
        var user = await CreateAuthenticatedUser();
        await CreateAuction(seller: user, name: "Lots of Panola", endDate: DateTimeOffset.Now.AddHours(2));
        await CreateAuction(seller: user, name: "A dungeon", endDate: DateTimeOffset.Now.AddHours(-1));
        await CreateAuction(seller: user, name: "Puffy Dog", endDate: DateTimeOffset.Now.AddHours(1));

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/seller");

        response.ShouldNotBeNull();
        response.Auctions[0].Name.ShouldBe("Lots of Panola");
        response.Auctions[1].Name.ShouldBe("Puffy Dog");
        response.Auctions[2].Name.ShouldBe("A dungeon");
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionDetails()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "test.bucket.com");
        var user = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
            seller: user,
            name: "Test Auction",
            description: "A test item",
            initialPrice: 50.00m,
            endDate: DateTimeOffset.Now.AddHours(1)
        );

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/seller");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.First(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.Name.ShouldBe("Test Auction");
        auctionResponse.CurrentPrice.ShouldBe(50.00m);
        auctionResponse.ImageHref.ShouldBe($"https://test.bucket.com/auctions/{auction.Image}");
        auctionResponse.EndDate.ShouldBe(auction.EndDate);
        auctionResponse.IsOpen.ShouldBeTrue();
        auctionResponse.IsClosed.ShouldBeFalse();
        auctionResponse.IsCancelled.ShouldBeFalse();
    }
}
