using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.S3.Model;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BitsAndBobs.Features.Auctions.Endpoints;

public static class UploadImageEndpoint
{
    public sealed record AuctionImageResponse([property: Required] string Id, [property: Required] string Href);

    private static readonly Dictionary<string, string> ContentTypesAndExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "image/webp", ".webp" },
    };

    private static readonly IEnumerable<KeyValuePair<string, string[]>> InvalidContentType =
    [
        new("image", ["Unsupported image type; only JPEG, PNG and WebP are supported."]),
    ];

    public static async Task<Results<Ok<AuctionImageResponse>, ValidationProblem>> UploadImage(
        IFormFile file,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IOptions<AwsResourceOptions> options,
        [FromServices] IDynamoDBContext dynamo,
        [FromServices] IAmazonS3 s3
    )
    {
        if (!ContentTypesAndExtensions.TryGetValue(file.ContentType, out var extension))
            return TypedResults.ValidationProblem(InvalidContentType);

        var image = new AuctionImage(extension, claimsPrincipal.GetUserId());

        await dynamo.SaveItem(image);

        await using var stream = file.OpenReadStream();
        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = options.Value.AppBucketName,
                Key = $"auctions/{image.FileName}",
                InputStream = stream,
            }
        );

        return TypedResults.Ok(
            new AuctionImageResponse(image.Id.FriendlyValue, options.Value.GetAuctionImageHref(image.FileName))
        );
    }
}
