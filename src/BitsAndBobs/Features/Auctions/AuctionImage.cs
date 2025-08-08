using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.DynamoDb;
using StronglyTypedIds;

namespace BitsAndBobs.Features.Auctions;

[StronglyTypedId]
public readonly partial struct AuctionImageId
{
    private static partial string Prefix => "auctionimage#";
}

[DynamoDBTable(BitsAndBobsTable.Name)]
public class AuctionImage : VersionedEntity
{
    public const string SortKey = "AuctionImage";

    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public AuctionImage()
    {
    }

    /// <summary>
    /// Creates a new instance of the AuctionImage class with the specified file extension
    /// </summary>
    public AuctionImage(string fileExtension, UserId userId)
    {
        Id = AuctionImageId.Create();
        FileName = $"{Id.FriendlyValue}{fileExtension}";
        UserId = userId;
        UpdateVersion();
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK", typeof(AuctionImageId.DynamoConverter))]
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public AuctionImageId Id { get; protected set; }

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
    // This is a string rather than an AuctionId to allow for the "none" value.
    public string AuctionId { get; protected set; } = "none";

    /// <summary>
    /// Gets the file name
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Gets the ID of the user who uploaded the image
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId UserId { get; protected set; }

    /// <summary>
    /// Gets the created date of the image
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets whether the image has an associated auction
    /// </summary>
    public bool IsAssociatedWithAuction => AuctionId != "none";

    // This is here as the property is the hash key of a GSI and the AWS Document Model gets upset without it.
    // "Value cannot be null. (Parameter 'key')"
    // ReSharper disable once UnusedMember.Global
    [DynamoDBIgnore]
    protected string? RecipientUserId { get; set; }

    /// <summary>
    /// Sets the ID of the auction to which the image belongs.
    /// </summary>
    public void AssociateWithAuction(Auction auction)
    {
        if (IsAssociatedWithAuction)
            throw new InvalidOperationException("This image is already associated with an auction.");

        AuctionId = auction.Id.Value;
        UpdateVersion();
    }
}
