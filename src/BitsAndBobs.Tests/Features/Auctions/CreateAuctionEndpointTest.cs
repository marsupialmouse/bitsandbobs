using System.Net.Http.Json;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class CreateAuctionEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenCreatingAuctionWithoutAuthentication()
    {
        var request = CreateValidRequest();

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForEmptyName()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { Name = "" };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForEmptyDescription()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { Description = "" };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForZeroInitialPrice()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { InitialPrice = 0 };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForNegativeInitialPrice()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { InitialPrice = -10 };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForSmallBidIncrement()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { BidIncrement = 0.05m };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForPastEndDate()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { Period = TimeSpan.FromMinutes(-1) };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForEndDateTooSoon()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { Period = TimeSpan.FromMinutes(5) };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForEndDateTooFuturistic()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { Period = TimeSpan.FromDays(2) };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForNonExistentImage()
    {
        await CreateAuthenticatedUser();
        var request = CreateValidRequest() with { ImageId = AuctionImageId.Create().FriendlyValue };

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForImageOwnedByDifferentUser()
    {
        await CreateAuthenticatedUser();
        var image = await CreateTestImage(UserId.Create());
        var request = CreateValidRequest(image);

        var response = await HttpClient.PostAsJsonAsync("/api/auctions", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldCreateAuctionSuccessfully()
    {
        var user = await CreateAuthenticatedUser();
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image);

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        httpResponse.IsSuccessStatusCode.ShouldBeTrue();
        response.ShouldNotBeNull();
        response.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task ShouldAddAuctionToDatabase()
    {
        var user = await CreateAuthenticatedUser();
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image);

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        var auction = await GetAuctionFromDb(response!.Id);
        auction.ShouldNotBeNull();
        auction.Name.ShouldBe(request.Name);
        auction.Description.ShouldBe(request.Description);
        auction.Image.ShouldBe(image.FileName);
        auction.InitialPrice.ShouldBe(request.InitialPrice);
        auction.BidIncrement.ShouldBe(request.BidIncrement);
        auction.EndDate.ShouldBe(DateTimeOffset.Now.Add(request.Period), TimeSpan.FromSeconds(1));
        auction.SellerId.ShouldBe(user.Id);
        auction.Status.ShouldBe(AuctionStatus.Open);
        auction.CreatedDate.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldUpdateImageWithAuctionId()
    {
        var user = await CreateAuthenticatedUser();
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image);

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        var updatedImage = await GetImageFromDb(image.Id);
        updatedImage.ShouldNotBeNull();
        updatedImage.IsAssociatedWithAuction.ShouldBeTrue();
        updatedImage.AuctionId.ShouldBe(AuctionId.Parse(response!.Id).Value);
    }

    [Test]
    public async Task ShouldReturnFriendlyAuctionId()
    {
        var user = await CreateAuthenticatedUser();
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image);

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        response.ShouldNotBeNull();
        response.Id.ShouldBe(AuctionId.Parse(response.Id).FriendlyValue);
    }

    [Test]
    public async Task ShouldSetCurrentPriceToInitialPrice()
    {
        var user = await CreateAuthenticatedUser();
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image) with { InitialPrice = 99.99m };

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        var auction = await GetAuctionFromDb(response!.Id);
        auction!.CurrentPrice.ShouldBe(99.99m);
    }

    [Test]
    public async Task ShouldSetSellerDisplayName()
    {
        var user = await CreateAuthenticatedUser(u => u.DisplayName = "Shonk Supreme");
        var image = await CreateTestImage(user.Id);
        var request = CreateValidRequest(image);

        var httpResponse = await HttpClient.PostAsJsonAsync("/api/auctions", request);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateAuctionEndpoint.CreateAuctionResponse>();

        var auction = await GetAuctionFromDb(response!.Id);
        auction!.SellerDisplayName.ShouldBe(user.DisplayName);
    }

    private static CreateAuctionEndpoint.CreateAuctionRequest CreateValidRequest(AuctionImage? image = null) =>
        new(
            Name: "Test Auction",
            Description: "A test auction item",
            ImageId: image?.Id.FriendlyValue ?? AuctionImageId.Create().FriendlyValue,
            InitialPrice: 10.00m,
            BidIncrement: 1.00m,
            Period: TimeSpan.FromMinutes(30)
        );

    private static async Task<AuctionImage> CreateTestImage(UserId userId)
    {
        var image = new AuctionImage(".jpg", userId);
        await DynamoContext.SaveItem(image);
        return image;
    }

    private static Task<AuctionImage?> GetImageFromDb(AuctionImageId id) =>
        DynamoContext.LoadAsync<AuctionImage>(id.Value, AuctionImage.SortKey)!;
}
