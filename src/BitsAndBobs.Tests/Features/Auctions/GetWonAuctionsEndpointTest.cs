using System.Net.Http.Json;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class GetWonAuctionsEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn401WhenNotAuthenticated()
    {
        var response = await HttpClient.GetAsync("/api/auctions/won");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturnEmptyCollectionWhenUserHasNoAuctions()
    {
        SetAuthenticatedClaimsPrincipal();

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/won");

        response.ShouldNotBeNull();
        response.Auctions.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReturnOnlyCompleteAuctionsWonByUser()
    {
        var endDate = DateTimeOffset.Now.AddHours(-1);
        var user = await CreateAuthenticatedUser();
        var auction1 = await CreateAuction();
        var auction2 = await CreateAuction();
        var auction3 = await CreateAuction();
        var auction4 = await CreateAuction();
        await AddBidToAuction(auction1, user.Id, 210m);
        await AddBidToAuction(auction2, user.Id, 222m);
        await AddBidToAuction(auction3, user.Id, 210m);
        await AddBidToAuction(auction4, UserId.Create(), 210m);
        await UpdateStatus(
            (auction1, AuctionStatus.Complete, endDate),
            (auction2, AuctionStatus.Complete, endDate),
            (auction3, AuctionStatus.Open, endDate),
            (auction4, AuctionStatus.Complete, endDate)
        );

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/won");

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(2);
        response.Auctions.ShouldContain(a => a.Id == auction1.Id.FriendlyValue);
        response.Auctions.ShouldContain(a => a.Id == auction2.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldNotIncludeCancelledOrEndedOpenAuctions()
    {
        var endDate = DateTimeOffset.Now.AddHours(-1);
        var user = await CreateAuthenticatedUser();
        var openAuction = await CreateAuction();
        var completeAuction = await CreateAuction();
        var cancelledAuction = await CreateAuction();
        await AddBidToAuction(openAuction, user.Id, 210m);
        await AddBidToAuction(completeAuction, user.Id, 211m);
        await AddBidToAuction(cancelledAuction, user.Id, 212m);
        await UpdateStatus(
            (openAuction, AuctionStatus.Open, endDate),
            (completeAuction, AuctionStatus.Complete, endDate),
            (cancelledAuction, AuctionStatus.Cancelled, endDate)
        );

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/won");

        response.ShouldNotBeNull();
        response.Auctions.Count.ShouldBe(1);
        response.Auctions[0].Id.ShouldBe(completeAuction.Id.FriendlyValue);
    }

    [Test]
    public async Task ShouldOrderAuctionsByEndDateDescending()
    {
        var now = DateTimeOffset.Now;
        var user = await CreateAuthenticatedUser();
        var auction1 = await CreateAuction(name: "Lots of Panola");
        var auction2 = await CreateAuction(name: "Pass the Parcel");
        var auction3 = await CreateAuction(name: "Puffy Dog");
        await AddBidToAuction(auction1, user.Id, 201m);
        await AddBidToAuction(auction2, user.Id, 202m);
        await AddBidToAuction(auction3, user.Id, 203m);
        await UpdateStatus(
            (auction1, AuctionStatus.Complete, now.AddHours(-2)),
            (auction2, AuctionStatus.Complete, now.AddHours(-1)),
            (auction3, AuctionStatus.Complete, now.AddHours(-3))
        );

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/won");

        response.ShouldNotBeNull();
        response.Auctions[0].Name.ShouldBe("Pass the Parcel");
        response.Auctions[1].Name.ShouldBe("Lots of Panola");
        response.Auctions[2].Name.ShouldBe("Puffy Dog");
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionDetails()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "test.bucket.com");
        var user = await CreateAuthenticatedUser();
        var endDate = DateTimeOffset.Now.AddMinutes(-1);
        var auction = await CreateAuction(
                          name: "Test Auction",
                          description: "A test item",
                          initialPrice: 50.00m,
                          bidIncrement: 1
                      );
        await AddBidsToAuction(auction, (UserId.Create(), 85), (user.Id, 85.34m));
        await UpdateStatus(auction, AuctionStatus.Complete, endDate);

        var response = await HttpClient.GetFromJsonAsync<UserAuctionsEndpoints.GetUserAuctionsResponse>("/api/auctions/won");

        var a = await GetAuctionFromDb(auction.Id);
        response.ShouldNotBeNull();
        var auctionResponse = response.Auctions.First(a => a.Id == auction.Id.FriendlyValue);
        auctionResponse.Name.ShouldBe("Test Auction");
        auctionResponse.CurrentPrice.ShouldBe(85.34m);
        auctionResponse.ImageHref.ShouldBe($"https://test.bucket.com/auctions/{auction.Image}");
        auctionResponse.EndDate.ShouldBe(endDate);
        auctionResponse.IsOpen.ShouldBeFalse();
        auctionResponse.IsClosed.ShouldBeTrue();
        auctionResponse.IsCancelled.ShouldBeFalse();
        auctionResponse.IsUserCurrentBidder.ShouldBeTrue();
    }

    private static Task UpdateStatus(Auction auction, AuctionStatus status, DateTimeOffset endDate) =>
        UpdateStatus((auction, status, endDate));

    private static Task UpdateStatus(params (Auction auction, AuctionStatus status, DateTimeOffset endDate)[] auctions)
    {
        return Testing.DynamoClient.TransactWriteItemsAsync(
            new TransactWriteItemsRequest
            {
                TransactItems = auctions.Select(x => UpdateItem(x.auction, x.status, x.endDate)).ToList()
            }
        );

        static TransactWriteItem UpdateItem(Auction a, AuctionStatus s, DateTimeOffset d) => new()
        {
            Update = new Update
            {
                TableName = BitsAndBobsTable.FullName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue(a.Id.Value) },
                    { "SK", new AttributeValue(Auction.SortKey) },
                },
                UpdateExpression = "SET AuctionStatus = :status, EndDate = :endDate",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":status", new AttributeValue { N = ((int)s).ToString() } },
                    { ":endDate", new AttributeValue(d.ToString("O")) },
                },
            }
        };
    }
}
