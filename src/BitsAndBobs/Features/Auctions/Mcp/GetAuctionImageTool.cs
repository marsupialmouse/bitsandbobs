using System.ComponentModel;
using Amazon.S3;
using Amazon.S3.Model;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetAuctionImageTool
{
    [McpServerTool, Description("Gets the image for an auction by auction ID")]
    public static async Task<object> GetAuctionImage(
        string auctionId,
        [FromServices] AuctionService auctionService,
        [FromServices] IOptions<AwsResourceOptions> options,
        [FromServices] IAmazonS3 s3
    )
    {
        if (!AuctionId.TryParse(auctionId, out var id))
            return Results.NotFound();

        var auction = await auctionService.GetAuction(id);

        if (auction is null)
            return Results.NotFound();

        using var image = await s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = options.Value.AppBucketName,
                Key = $"auctionimages/{auction.Image}",
            }
        );

        var fileType = auction.Image.Split('.').Last();

        await using var stream = image.ResponseStream;
        var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // We return it as a base64 string as the framework seems to want to return everything as JSON
        return new { image = $"data:image/{fileType};base64,{Convert.ToBase64String(memoryStream.ToArray())}" };
    }
}
