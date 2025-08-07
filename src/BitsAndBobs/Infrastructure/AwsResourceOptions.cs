using System.ComponentModel.DataAnnotations;

namespace BitsAndBobs.Infrastructure;

public class AwsResourceOptions
{
    public const string SectionName = "Aws:Resources";

    /// <summary>
    /// Gets the name of the S3 bucket used for auction images.
    /// </summary>
    [Required]
    public string AppBucketName { get; set; } = "";

    /// <summary>
    /// Gets the domain name of the S3 bucket used for auction images. If this is null or empty we assume the  images
    /// are hosted on the same domain as the API (e.g. via CloudFront).
    /// </summary>
    public string AppBucketDomainName { get; set; } = "";

    /// <summary>
    /// Gets the Href for an auction image based on the current settings.
    /// </summary>
    public string GetAuctionImageHref(string fileName) =>
        string.IsNullOrEmpty(AppBucketDomainName)
            ? $"/auctions/{fileName}"
            : $"https://{AppBucketDomainName}/auctions/{fileName}";

}
