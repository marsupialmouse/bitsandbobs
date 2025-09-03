using System.ComponentModel;
using Amazon.S3;
using Amazon.S3.Model;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace BitsAndBobs.Features.Auctions.Mcp;

[McpServerToolType]
public class GetAuctionImageTool
{
    [McpServerTool, Description("Gets the image for an auction by auction ID")]
    public static async Task<ContentBlock> GetAuctionImage(
        string auctionId,
        [FromServices] AuctionService auctionService,
        [FromServices] IOptions<AwsResourceOptions> options,
        [FromServices] IAmazonS3 s3
    )
    {
        if (!AuctionId.TryParse(auctionId, out var id))
            throw new McpException("Invalid auction ID", McpErrorCode.InvalidParams);

        var auction = await auctionService.GetAuction(id);

        if (auction is null)
            throw new McpException("Auction not found");

        using var image = await s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = options.Value.AppBucketName,
                Key = $"auctionimages/{auction.Image}",
            }
        );

        await using var stream = image.ResponseStream;
        var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return new ImageContentBlock
        {
            Data = Convert.ToBase64String(memoryStream.ToArray()),
            MimeType = $"image/{auction.Image.Split('.').Last()}",
        };
    }
}
