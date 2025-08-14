using System.Net.Http.Json;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

[TestFixture]
public class GetAuctionsEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldNotReturnCancelledAuctions()
    {
        var cancelledAuction = await CreateAuction(
                                   name: "Cancelled Auction",
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
        var pastAuction = await CreateAuction(name: "Past Auction", endDate: DateTimeOffset.Now.AddHours(-1));

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        response.Auctions.ShouldNotContain(a => a.Id == pastAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldReturnOpenFutureAuctions()
    {
        var futureAuction = await CreateAuction(name: "Future Auction", endDate: DateTimeOffset.Now.AddHours(1));

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var returnedAuction = response.Auctions.FirstOrDefault(a => a.Id == futureAuction.Id.FriendlyValue);
        returnedAuction.ShouldNotBeNull();
        returnedAuction.Name.ShouldBe("Future Auction");
    }

    [Test]
    public async Task ShouldOrderAuctionsByEndDateWithEarliestFirst()
    {
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        await CreateAuction(name: $"{uniquePrefix}-Soon", endDate: DateTimeOffset.Now.AddMinutes(30));
        await CreateAuction(name: $"{uniquePrefix}-Later", endDate: DateTimeOffset.Now.AddHours(2));

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
        var seller = await CreateUser(displayName: "David Jones");
        var uniqueName = $"Test Item {Guid.NewGuid():N}";
        var auction = await CreateAuction(
            seller: seller,
            name: uniqueName,
            description: "A wonderful test item",
            initialPrice: 50.00m,
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
        auctionResponse.NumberOfBids.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturnImageUrlWithDomainWhenSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "test.bucket.com");
        var auction = await CreateAuction();

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.FirstOrDefault(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.ShouldNotBeNull();
        auctionResponse.ImageHref.ShouldBe($"https://test.bucket.com/auctions/{auction.Image}");
    }

    [Test]
    public async Task ShouldReturnImagePathWhenNoDomainSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "");
        var auction = await CreateAuction();

        var response = await HttpClient.GetFromJsonAsync<GetAuctionsEndpoint.GetAuctionsResponse>("/api/auctions");

        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.FirstOrDefault(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.ShouldNotBeNull();
        auctionResponse.ImageHref.ShouldBe($"/auctions/{auction.Image}");
    }
}
