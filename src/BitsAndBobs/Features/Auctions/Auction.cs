using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;
using StronglyTypedIds;

namespace BitsAndBobs.Features.Auctions;

[StronglyTypedId]
public readonly partial struct AuctionId
{
    private static partial string Prefix => "auction#";
}

[DynamoDBTable(BitsAndBobsTable.Name)]
public class Auction : VersionedEntity
{
    public const string SortKey = "Auction";

    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedMember.Global
    public Auction()
    {
    }

    /// <summary>
    /// Creates a new auction lot.
    /// </summary>
    public Auction(User seller, string name, string description, AuctionImage image, decimal initialPrice, decimal bidIncrement, TimeSpan period)
    {
        Id = AuctionId.Create();
        Name = name;
        Description = description;
        Image = image.FileName;
        InitialPrice = initialPrice;
        CurrentPrice = initialPrice;
        BidIncrement = bidIncrement;
        EndDate = DateTimeOffset.Now.Add(period);
        SellerId = seller.Id;
        SellerDisplayName = seller.DisplayName;

        image.AssociateWithAuction(this);

        UpdateVersion();
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK", typeof(AuctionId.DynamoConverter))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global (DynamoDB)
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public AuctionId Id { get; protected set; }

    // ReSharper disable once UnusedMember.Global (DynamoDB)
    // ReSharper disable once InconsistentNaming
    protected string SK
    {
        get => SortKey;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set { }
    }

    /// <summary>
    /// Gets or sets the name of the item
    /// </summary>
    public string Name { get; protected set; } = "";

    /// <summary>
    /// Gets or sets the description of the item
    /// </summary>
    public string Description { get; protected set; } = "";

    /// <summary>
    /// Gets or sets the name of the image of the item
    /// </summary>
    public string Image { get; protected set; } = "";

    /// <summary>
    /// Gets or sets the initial price of the lot
    /// </summary>
    public decimal InitialPrice { get; protected set; }

    /// <summary>
    /// Gets or sets the increment for each bid
    /// </summary>
    public decimal BidIncrement { get; protected set; }

    /// <summary>
    /// Gets the created date of the lot
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedDate { get; protected set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets the end date of the auction
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset EndDate { get; protected set; }


    // ReSharper disable once UnusedMember.Global (this is here for a GSI)
    protected long EndDateUtcTimeStamp
    {
        get => EndDate.UtcTicks;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        set { }
    }

   [DynamoDBProperty("AuctionStatus")]
    public AuctionStatus Status { get; protected set; } = AuctionStatus.Open;

    /// <summary>
    /// Whether the auction is currently open for bidding
    /// </summary>
    public bool IsOpen => Status == AuctionStatus.Open && EndDate > DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the user ID of the seller
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId SellerId { get; protected set; }

    /// <summary>
    /// Gets or sets the seller's display name
    /// </summary>
    public string SellerDisplayName { get; protected set; } = "";

    /// <summary>
    /// Gets or sets the current price of the lot
    /// </summary>
    public decimal CurrentPrice { get; protected set; }

    /// <summary>
    /// Gets or sets the user ID of the current winning bidder
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId? CurrentBidderId { get; protected set; }

    // This is here as the property is the hash key of a GSI and the AWS Document Model gets upset without it.
    // "Value cannot be null. (Parameter 'key')"
    // ReSharper disable once UnusedMember.Global
    [DynamoDBIgnore]
    protected string? RecipientUserId { get; set; }

    public void Cancel()
    {
        if (!IsOpen)
            throw new InvalidOperationException("Cannot cancel an auction that is not open.");

        Status = AuctionStatus.Cancelled;
        UpdateVersion();
    }
}
