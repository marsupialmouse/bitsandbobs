using System.Net.Http.Json;
using Amazon.S3.Model;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using BitsAndBobs.Features.Identity;
using NSubstitute;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

public class GetAuctionForRelistingEndpointTest : AuctionTestBase
{
    [Test]
    public async Task ShouldReturn401WhenNotAuthenticated()
    {
        var auction = await CreateAuction( configure: c => c.Cancel());

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/relist", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldReturn404WhenAuctionDoesNotExist()
    {
        SetAuthenticatedClaimsPrincipal();
        var nonExistentId = AuctionId.Create().FriendlyValue;

        var response = await HttpClient.PostAsync($"/api/auctions/{nonExistentId}/relist", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldReturn400WhenUserIsNotTheSeller()
    {
        SetAuthenticatedClaimsPrincipal();
        var auction = await CreateAuction(configure: c => c.Cancel());

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/relist", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldReturn400WhenAuctionIsOpen()
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(seller: seller);

        var response = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/relist", null);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [TestCase(AuctionStatus.Cancelled, 0)]
    [TestCase(AuctionStatus.Complete, -20)]
    public async Task ShouldReturnDetailsForClosedAuction(AuctionStatus status, int dateIncrement)
    {
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(
                          seller: seller,
                          name: "Sliced Bread",
                          description: "Such thin slices",
                          initialPrice: 743.12m,
                          bidIncrement: 1.59m,
                          endDate: DateTimeOffset.Now.AddMinutes(15)
                      );
        await AddBidsToAuction(auction, (UserId.Create(), 800m), (UserId.Create(), 900m));
        await UpdateStatus(auction, AuctionStatus.Cancelled, auction.EndDate.AddMinutes(dateIncrement));

        var httpResponse = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/relist", null);
        var response = await httpResponse.Content.ReadFromJsonAsync<GetAuctionForRelistingEndpoint.GetAuctionForRelistingResponse>();

        httpResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.ShouldNotBeNull();
        response.Name.ShouldBe(auction.Name);
        response.Description.ShouldBe(auction.Description);
        response.InitialPrice.ShouldBe(auction.InitialPrice);
        response.BidIncrement.ShouldBe(auction.BidIncrement);
    }

    [Test]
    public async Task ShouldCopyAuctionImageAndReturnDetails()
    {
        UpdateSetting("AWS:Resources:AppBucketName", "grandma-josephine");
        var seller = await CreateAuthenticatedUser();
        var auction = await CreateAuction(seller: seller, imageExtension: ".webp", configure: c => c.Cancel());

        var httpResponse = await HttpClient.PostAsync($"/api/auctions/{auction.Id.FriendlyValue}/relist", null);
        var response = await httpResponse.Content.ReadFromJsonAsync<GetAuctionForRelistingEndpoint.GetAuctionForRelistingResponse>();

        httpResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.ShouldNotBeNull();
        var image = await GetImageFromDb(response.ImageId);
        image.ShouldNotBeNull();
        response.ImageHref.ShouldBe($"/auctionimages/{image.FileName}");
        await S3Client
              .Received(1)
              .CopyObjectAsync(
                  Arg.Is<CopyObjectRequest>(req => req.SourceBucket == "grandma-josephine"
                                                   && req.DestinationBucket == "grandma-josephine"
                                                   && req.SourceKey == $"auctionimages/{auction.Image}"
                                                   && req.DestinationKey == $"auctionimages/{image.FileName}"
                  )
              );
    }

    private static Task<AuctionImage?> GetImageFromDb(string id) =>
        DynamoContext.LoadAsync<AuctionImage>(AuctionImageId.Parse(id).Value, AuctionImage.SortKey)!;
}
