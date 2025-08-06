using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Auctions;

[DynamoDBTable(BitsAndBobsTable.Name)]
public class Auction
{
    public const string SortKey = "Auction";

    protected Auction()
    {
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK")]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global (DynamoDB)
    public string Id { get; protected set; } = $"auction#{Guid.NewGuid():n}";

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
    public string Name { get; protected set; }

    /// <summary>
    /// Gets or sets the description of the item
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    /// Gets or sets the name of the image of the item
    /// </summary>
    public string Image { get; protected set; }

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

    [DynamoDBProperty("AuctionStatus")]
    public AuctionStatus Status { get; protected set; } = AuctionStatus.Open;

    /// <summary>
    /// Whether the auction is currently open for bidding
    /// </summary>
    public bool IsOpen => Status == AuctionStatus.Open && EndDate > DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the user ID of the seller
    /// </summary>
    public string SellerId { get; protected set; } = "";

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
    public string? CurrentBidderId { get; protected set; }

    /// <summary>
    /// Gets the version string (for concurrency control)
    /// </summary>
    public string Version { get; protected set; } = "";

    /// <summary>
    /// Gets the version string before the last update (for concurrency control)
    /// </summary>
    [DynamoDBIgnore]
    public string InitialVersion { get; protected set; } = "";

    public static Auction Create(string name, string description, AuctionImage image, decimal initialPrice, decimal bidIncrement, DateTimeOffset endDate, User seller)
    {
        var auction = new Auction
        {
            Name = name,
            Description = description,
            Image = image.FileName,
            InitialPrice = initialPrice,
            BidIncrement = bidIncrement,
            EndDate = endDate,
            SellerId = seller.Id,
            SellerDisplayName = seller.DisplayName,
            CurrentPrice = initialPrice,
            Version = Guid.NewGuid().ToString()
        };
        image.AssociateWithAuction(auction);
        auction.UpdateVersion();
        return auction;
    }

    public void Cancel()
    {
        if (!IsOpen)
            throw new InvalidOperationException("Cannot cancel an auction that is not open.");

        Status = AuctionStatus.Cancelled;
        UpdateVersion();
    }

    private void UpdateVersion()
    {
        InitialVersion = Version;
        Version = Guid.NewGuid().ToString("n");
    }
}
