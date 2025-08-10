using System.Net.Http.Json;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class GetAuctionsEndpointTest : TestBase
{
    [Test]
    public async Task ShouldNotReturnCancelledAuctions()
    {
        var user = await CreateUser();
        var cancelledAuction = await CreateAuction(
                                   user,
                                   "Cancelled Auction",
                                   endDate: DateTimeOffset.Now.AddHours(1),
                                   configure: a => a.Cancel()
                               );

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        response.Auctions.ShouldNotContain(a => a.Id == cancelledAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldNotReturnPastAuctions()
    {
        var user = await CreateUser();
        var pastAuction = await CreateAuction(user, "Past Auction", endDate: DateTimeOffset.Now.AddHours(-1));

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        response.Auctions.ShouldNotContain(a => a.Id == pastAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldReturnOpenFutureAuctions()
    {
        var user = await CreateUser();
        var futureAuction = await CreateAuction(user, "Future Auction", endDate: DateTimeOffset.Now.AddHours(1));

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var returnedAuction = response.Auctions.FirstOrDefault(a => a.Id == futureAuction.Id.FriendlyValue);
        returnedAuction.ShouldNotBeNull();
        returnedAuction.Name.ShouldBe("Future Auction");
    }

    [Test]
    public async Task ShouldOrderAuctionsByEndDateWithEarliestFirst()
    {
        var user = await CreateUser();
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        await CreateAuction(user, $"{uniquePrefix}-Soon", endDate: DateTimeOffset.Now.AddMinutes(30));
        await CreateAuction(user, $"{uniquePrefix}-Later", endDate: DateTimeOffset.Now.AddHours(2));

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var ourAuctions = response.Auctions.Where(a => a.Name.StartsWith(uniquePrefix)).ToList();
        ourAuctions.Count.ShouldBe(2);
        ourAuctions[0].Name.ShouldBe($"{uniquePrefix}-Soon");
        ourAuctions[1].Name.ShouldBe($"{uniquePrefix}-Later");
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionData()
    {
        var user = await CreateUser(u => u.DisplayName = "David Jones");
        var uniqueName = $"Test Item {Guid.NewGuid():N}";
        var auction = await CreateAuction(
            user,
            uniqueName,
            "A wonderful test item",
            currentPrice: 50.00m,
            endDate: DateTimeOffset.Now.AddHours(1)
        );

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.FirstOrDefault(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.ShouldNotBeNull();
        auctionResponse.Id.ShouldBe(auction.Id.FriendlyValue);
        auctionResponse.Name.ShouldBe(uniqueName);
        auctionResponse.Description.ShouldBe("A wonderful test item");
        auctionResponse.CurrentPrice.ShouldBe(50.00m);
        auctionResponse.SellerDisplayName.ShouldBe("David Jones");
        auctionResponse.EndDate.ShouldBe(auction.EndDate);
    }

    [Test]
    public async Task ShouldReturnImageUrlWithDomainWhenSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "test.bucket.com");
        var user = await CreateUser();
        var auction = await CreateAuction(user, $"Test Auction {Guid.NewGuid():N}");

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.FirstOrDefault(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.ShouldNotBeNull();
        auctionResponse.ImageUrl.ShouldBe($"https://test.bucket.com/auctions/{auction.Image}");
    }

    [Test]
    public async Task ShouldReturnImagePathWhenNoDomainSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "");
        var user = await CreateUser();
        var auction = await CreateAuction(user, $"Test Auction {Guid.NewGuid():N}");

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.FirstOrDefault(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.ShouldNotBeNull();
        auctionResponse.ImageUrl.ShouldBe($"/auctions/{auction.Image}");
    }

    private static async Task<Auction> CreateAuction(
        User seller,
        string name,
        string description = "Test description",
        decimal currentPrice = 25.00m,
        DateTimeOffset? endDate = null,
        Action<Auction>? configure = null)
    {
        endDate ??= DateTimeOffset.Now.AddHours(24);

        var auction = new Auction(
            seller,
            name,
            description,
            new AuctionImage(".jpg", seller.Id),
            currentPrice,
            1.00m,
            endDate.Value - DateTimeOffset.Now
        );

        configure?.Invoke(auction);
        await DynamoContext.SaveItem(auction);
        return auction;
    }
}
