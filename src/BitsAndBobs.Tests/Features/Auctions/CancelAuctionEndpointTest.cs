using BitsAndBobs.Features.Auctions;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class CancelAuctionEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn401WhenNotAuthenticated()
    {
        var auction = await CreateAuction(name: "Haunted Toaster", description: "Makes bread disappear completely");

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturn404WhenAuctionDoesNotExist()
    {
        SetAuthenticatedClaimsPrincipal();
        var nonExistentId = AuctionId.Create().FriendlyValue;

        var response = await HttpClient.PostAsync($"/api/auctions/{nonExistentId}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldReturn400WhenUserIsNotTheSeller()
    {
        SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(
                          name: "Rubber Duck Army",
                          description: "500 yellow rubber ducks, slightly used in bathtub warfare"
                      );

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturn400WhenAuctionIsAlreadyCancelled()
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
            seller: seller,
            name: "Time Machine (Slightly Broken)",
            description: "Only goes backwards to last Tuesday",
            configure: a => a.Cancel()
        );

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturn400WhenAuctionHasAlreadyEnded()
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
            seller: seller,
            name: "Crystal Ball (Cloudy)",
            description: "Predicts the past with 90% accuracy",
            endDate: DateTimeOffset.Now.AddHours(-2)
        );

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldCancelAuctionSuccessfully()
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
            seller: seller,
            name: "Invisible Cloak",
            description: "Works perfectly, as evidenced by the fact you can't see it"
        );

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task ShouldUpdateAuctionStatusInDatabase()
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
            seller: seller,
            name: "Self-Stirring Cauldron",
            description: "Stirs itself, but unfortunately only counterclockwise"
        );

        await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/cancel", null);

        var updatedAuction = await GetAuctionFromDb(auction.Id);
        updatedAuction.ShouldNotBeNull();
        updatedAuction.Status.ShouldBe(AuctionStatus.Cancelled);
        updatedAuction.CancelledDate.ShouldNotBeNull();
        updatedAuction.CancelledDate.Value.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        updatedAuction.IsOpen.ShouldBe(false);
    }
}
