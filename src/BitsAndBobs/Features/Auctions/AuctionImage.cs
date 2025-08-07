using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Auctions;

[DynamoDBTable(BitsAndBobsTable.Name)]
public class AuctionImage
{
    public const string SortKey = "AuctionImage";

    [Obsolete("This constructor is for DynamoDB only and should not be used directly.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public AuctionImage()
    {
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK")]
    public string Id { get; protected set; } = "";

    protected string SK
    {
        get => SortKey;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set { }
    }

    /// <summary>
    /// Gets or sets the ID of thr auction this image belongs to (or "none" if it is not associated with an auction)
    /// </summary>
    public string AuctionId { get; protected set; } = "none";

    /// <summary>
    /// Gets the file name
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Gets the ID of the user who uploaded the image
    /// </summary>
    public string UserId { get; protected set; } = "";

    /// <summary>
    /// Gets the created date of the image
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets whether the image has an associated auction
    /// </summary>
    public bool IsAssociatedWithAuction => AuctionId != "none";

    /// <summary>
    /// Gets the version string (for concurrency control)
    /// </summary>
    public string Version { get; protected set; } = "";

    /// <summary>
    /// Gets the version string before the last update (for concurrency control)
    /// </summary>
    [DynamoDBIgnore]
    public string InitialVersion { get; protected set; } = "";

    // This is here as the property is the hash key of a GSI and the AWS Document Model gets upset without it.
    // ReSharper disable once UnusedMember.Global
    [DynamoDBIgnore]
    protected string? RecipientUserId { get; set; }

    /// <summary>
    /// Creates a new instance of the AuctionImage class with the specified file name
    /// </summary>
    public static AuctionImage Create(string fileExtension, ClaimsPrincipal user)
    {
        var id = Guid.NewGuid().ToString("n");
        var image = new AuctionImage
        {
            Id = $"auctionimage#{id}",
            FileName = $"{id}{fileExtension}",
            UserId = user.GetUserId(),
        };
        image.UpdateVersion();
        return image;
    }

    /// <summary>
    /// Sets the ID of the auction to which the image belongs.
    /// </summary>
    public void AssociateWithAuction(Auction auction)
    {
        if (IsAssociatedWithAuction)
            throw new InvalidOperationException("This image is already associated with an auction.");

        AuctionId = auction.Id;
        UpdateVersion();
    }

    private void UpdateVersion()
    {
        InitialVersion = Version;
        Version = Guid.NewGuid().ToString("n");
    }
}
