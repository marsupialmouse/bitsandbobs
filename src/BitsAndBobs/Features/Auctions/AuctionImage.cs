using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Auctions;

[DynamoDBTable(BitsAndBobsTable.Name)]
public class AuctionImage
{
    public const string SortKey = "AuctionImage";

    // Serialization constructor for DynamoDB
    // ReSharper disable once MemberCanBePrivate.Global
    protected AuctionImage()
    {
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK")]
    public string Id { get; protected set; } = $"auctionimage#{Guid.NewGuid():n}";

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
    /// Gets or sets the file name
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Gets the created date of the image
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets whether the image has an associated auction
    /// </summary>
    public bool HasAuction => AuctionId != "none";

    /// <summary>
    /// Gets the version string (for concurrency control)
    /// </summary>
    public string Version { get; protected set; } = "";

    /// <summary>
    /// Gets the version string before the last update (for concurrency control)
    /// </summary>
    [DynamoDBIgnore]
    public string InitialVersion { get; protected set; } = "";

    /// <summary>
    /// Creates a new instance of the AuctionImage class with the specified file name
    /// </summary>
    public static AuctionImage Create(string fileName)
    {
        var image = new AuctionImage { FileName = fileName };
        image.UpdateVersion();
        return image;
    }

    /// <summary>
    /// Sets the ID of the auction to which the image belongs.
    /// </summary>
    public void AssociateWithAuction(Auction auction)
    {
        if (HasAuction)
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
