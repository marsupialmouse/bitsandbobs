using System.Net.Http.Json;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class GetAuctionEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn404WhenAuctionDoesNotExist()
    {
        var nonExistentId = AuctionId.Create().FriendlyValue;

        var response = await HttpClient.GetAsync($"/api/auctions/{nonExistentId}");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldReturnAuctionDetailsForExistingAuction()
    {
        var seller = new User { DisplayName = "Panola" };
        var auction = await CreateAuction(
            seller: seller,
            name: "Vintage Guitar",
            description: "A beautiful vintage guitar",
            initialPrice: 500m,
            endDate: DateTimeOffset.Now.AddHours(2)
        );

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.Id.ShouldBe(auction.Id.FriendlyValue);
        response.Name.ShouldBe("Vintage Guitar");
        response.Description.ShouldBe("A beautiful vintage guitar");
        response.SellerDisplayName.ShouldBe("Panola");
        response.InitialPrice.ShouldBe(500m);
        response.CurrentPrice.ShouldBe(500m);
        response.NumberOfBids.ShouldBe(0);
        response.CurrentBidderDisplayName.ShouldBeNull();
        response.EndDate.ShouldBe(auction.EndDate);
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionStatusForOpenAuction()
    {
        var auction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(1));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.IsOpen.ShouldBeTrue();
        response.IsClosed.ShouldBeFalse();
        response.IsCancelled.ShouldBeFalse();
        response.CancelledDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionStatusForClosedAuction()
    {
        var auction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(-1));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.IsOpen.ShouldBeFalse();
        response.IsClosed.ShouldBeTrue();
        response.IsCancelled.ShouldBeFalse();
        response.CancelledDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnCorrectAuctionStatusForCancelledAuction()
    {
        var auction = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(1), configure: a => a.Cancel());

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.IsOpen.ShouldBeFalse();
        response.IsClosed.ShouldBeFalse();
        response.IsCancelled.ShouldBeTrue();
        response.CancelledDate.ShouldNotBeNull();
        response.CancelledDate.Value.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldReturnImageUrlWithDomainWhenSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "test.bucket.com");
        var auction = await CreateAuction();

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.ImageHref.ShouldBe($"https://test.bucket.com/auctions/{auction.Image}");
    }

    [Test]
    public async Task ShouldReturnImagePathWhenNoDomainSet()
    {
        UpdateSetting("AWS:Resources:AppBucketDomainName", "");
        var auction = await CreateAuction();

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.ImageHref.ShouldBe($"/auctions/{auction.Image}");
    }

    [Test]
    public async Task ShouldNotReturnBidsForUnauthenticatedUser()
    {
        var bidder = await CreateUser();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, bidder.Id, 150m);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.Bids.ShouldBeNull();
        response.NumberOfBids.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReturnBidsListForAuthenticatedUser()
    {
        SetAuthenticatedClaimsPrincipal();
        var bidder1 = await CreateUser(u => u.DisplayName = "Let Down");
        var bidder2 = await CreateUser(u => u.DisplayName = "Lucky");
        var bidder3 = await CreateUser(u => u.DisplayName = "Lift");
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 5m);
        await AddBidsToAuction(auction, (bidder1.Id, 100m), (bidder2.Id, 105m), (bidder3.Id, 110m), (bidder2.Id, 115m));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.CurrentBidderDisplayName.ShouldBe("Lucky");
        response.Bids.ShouldNotBeNull();
        response.Bids.Count.ShouldBe(4);
        var bids = response.Bids.OrderBy(x => x.Amount).ToList();
        bids[0].BidderDisplayName.ShouldBe("Let Down");
        bids[0].Amount.ShouldBe(100m);
        bids[0].IsCurrentBid.ShouldBeFalse();
        bids[0].IsUserBid.ShouldBeFalse();
        bids[1].BidderDisplayName.ShouldBe("Lucky");
        bids[1].Amount.ShouldBe(105m);
        bids[1].IsCurrentBid.ShouldBeFalse();
        bids[1].IsUserBid.ShouldBeFalse();
        bids[2].BidderDisplayName.ShouldBe("Lift");
        bids[2].Amount.ShouldBe(110m);
        bids[2].IsCurrentBid.ShouldBeFalse();
        bids[2].IsUserBid.ShouldBeFalse();
        bids[3].BidderDisplayName.ShouldBe("Lucky");
        bids[3].Amount.ShouldBe(115m);
        bids[3].IsCurrentBid.ShouldBeTrue();
        bids[3].IsUserBid.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldIndicateWhenUserIsTheSeller()
    {
        var seller = await CreateAuthenticatedUser(u => u.DisplayName = "(fadeout)");
        var auction = await CreateAuction(seller: seller);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.IsUserSeller.ShouldBe(true);
        response.IsUserCurrentBidder.ShouldBe(false);
    }

    [Test]
    public async Task ShouldIndicateWhenUserIsCurrentBidder()
    {
        var bidder = await CreateAuthenticatedUser();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, bidder.Id, 150m);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.IsUserSeller.ShouldBe(false);
        response.IsUserCurrentBidder.ShouldBe(true);
    }

    [Test]
    public async Task ShouldReturnCorrectMinimumBidWithoutExistingBids()
    {
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 25m);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.MinimumBid.ShouldBe(100m);
    }

    [Test]
    public async Task ShouldReturnCorrectMinimumBidWithExistingBid()
    {
        var bidder1 = await CreateUser();
        var bidder2 = await CreateUser();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 25m);
        await AddBidsToAuction(auction, (bidder1.Id, 100m), (bidder2.Id, 140m));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.MinimumBid.ShouldBe(150m);
    }

    [Test]
    public async Task ShouldReturnCorrectMinimumBidForCurrentBidderWithExistingBidLessThanNextIncrement()
    {
        var bidder1 = await CreateUser();
        var bidder2 = await CreateAuthenticatedUser();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 25m);
        await AddBidsToAuction(auction, (bidder1.Id, 100m), (bidder2.Id, 140m));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.MinimumBid.ShouldBe(150m);
    }

    [Test]
    public async Task ShouldReturnCorrectMinimumBidForCurrentBidderWithExistingBidGreaterThanNextIncrement()
    {
        var bidder1 = await CreateUser();
        var bidder2 = await CreateAuthenticatedUser();
        var auction = await CreateAuction(initialPrice: 100m, bidIncrement: 25m);
        await AddBidsToAuction(auction, (bidder1.Id, 100m), (bidder2.Id, 160m));

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.MinimumBid.ShouldBe(160.01m);
    }

    [Test]
    public async Task ShouldReturnCorrectFullBidAmountForUserOwnBid()
    {
        var bidder = await CreateAuthenticatedUser();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, bidder.Id, 150m);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.Bids.ShouldNotBeNull();
        response.Bids[0].IsUserBid.ShouldBeTrue();
        response.Bids[0].Amount.ShouldBe(150m);
    }

    [Test]
    public async Task ShouldHideFullCurrentBidAmountForOtherUsers()
    {
        SetAuthenticatedClaimsPrincipal();
        var bidder = await CreateUser();
        var auction = await CreateAuction(initialPrice: 100m);
        await AddBidToAuction(auction, bidder.Id, 150m);

        var response =
            await HttpClient.GetFromJsonAsync<GetAuctionEndpoint.GetAuctionResponse>(
                $"/api/auctions/{auction.Id.FriendlyValue}"
            );

        response.ShouldNotBeNull();
        response.Bids.ShouldNotBeNull();
        response.Bids[0].IsUserBid.ShouldBeFalse();
        response.Bids[0].Amount.ShouldBe(100m);
    }
}
