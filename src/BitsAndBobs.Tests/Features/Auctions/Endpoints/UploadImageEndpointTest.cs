using System.Net.Http.Json;
using Amazon.S3.Model;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using NSubstitute;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

[TestFixture]
public class UploadImageEndpointTest : TestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenUploadingWithoutAuthentication()
    {
        using var content = CreateImageContent("image/jpeg");

        var response = await HttpClient.PostAsync("/api/auctions/images", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        await S3Client.DidNotReceive().PutObjectAsync(Arg.Any<PutObjectRequest>());
    }

    [Test]
    public async Task ShouldReturnValidationProblemForUnsupportedImageType()
    {
        SetAuthenticatedClaimsPrincipal();
        using var content = CreateImageContent("image/gif");

        var response = await HttpClient.PostAsync("/api/auctions/images", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        await S3Client.DidNotReceive().PutObjectAsync(Arg.Any<PutObjectRequest>());
    }

    [TestCase("image/jpeg", ".jpg")]
    [TestCase("image/png", ".png")]
    [TestCase("image/webp", ".webp")]
    public async Task ShouldUploadImage(string contentType, string expectedExtension)
    {
        SetAuthenticatedClaimsPrincipal();

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent(contentType));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        httpResponse.IsSuccessStatusCode.ShouldBeTrue();
        response.ShouldNotBeNull();
        response.Href.ShouldEndWith(expectedExtension);
    }

    [Test]
    public async Task ShouldReturnImageResponseWithFullUrlHrefWhenDomainSet()
    {
        SetAuthenticatedClaimsPrincipal();
        UpdateSetting("AWS:Resources:AppBucketDomainName", "charlie.bucket");

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/jpeg"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        response.ShouldNotBeNull();
        response.Href.ShouldStartWith("https://charlie.bucket/auctions/");
        response.Href.ShouldEndWith(".jpg");
    }

    [Test]
    public async Task ShouldReturnImageResponseWithPathHrefWhenNoDomainSet()
    {
        SetAuthenticatedClaimsPrincipal();
        UpdateSetting("AWS:Resources:AppBucketDomainName", "");

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/png"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        response.ShouldNotBeNull();
        response.Href.ShouldStartWith("/auctions/");
        response.Href.ShouldEndWith(".png");
    }

    [Test]
    public async Task ShouldAddImageToDatabase()
    {
        var userId = SetAuthenticatedClaimsPrincipal();

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/jpeg"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        var image = await GetImageFromDb(response!.Id);
        image.ShouldNotBeNull();
        image.CreatedOn.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        image.AuctionId.ShouldBe("none");
        image.UserId.ShouldBe(userId);
        response.Href.ShouldEndWith(image.FileName);
    }

    [Test]
    public async Task ShouldReturnFriendlyImageId()
    {
        SetAuthenticatedClaimsPrincipal();

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/jpeg"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        response.ShouldNotBeNull();
        response.Id.ShouldBe(AuctionImageId.Parse(response.Id).FriendlyValue);
    }

    [Test]
    public async Task ShouldUseFriendlyIdInFileName()
    {
        SetAuthenticatedClaimsPrincipal();

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/webp"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        var image = await GetImageFromDb(response!.Id);
        image!.FileName.ShouldBe($"{image.Id.FriendlyValue}.webp");
    }


    [Test]
    public async Task ShouldAddImageToS3()
    {
        SetAuthenticatedClaimsPrincipal();
        UpdateSetting("AWS:Resources:AppBucketName", "grandma-georgina");

        var httpResponse = await HttpClient.PostAsync("/api/auctions/images", CreateImageContent("image/jpeg"));
        var response = await httpResponse.Content.ReadFromJsonAsync<UploadImageEndpoint.AuctionImageResponse>();

        var image = await GetImageFromDb(response!.Id);
        await S3Client
              .Received(1)
              .PutObjectAsync(
                  Arg.Is<PutObjectRequest>(req => req.BucketName == "grandma-georgina"
                                                  && req.Key == $"auctions/{image!.FileName}"
                                                  && req.InputStream != null
                  )
              );
    }

    private static MultipartFormDataContent CreateImageContent(string contentType)
    {
        var fileContent = "fake image content"u8.ToArray();
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(fileContent));
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", "test-image");
        return content;
    }

    private static Task<AuctionImage?> GetImageFromDb(string id) =>
        DynamoContext.LoadAsync<AuctionImage>(AuctionImageId.Parse(id).Value, AuctionImage.SortKey)!;
}
