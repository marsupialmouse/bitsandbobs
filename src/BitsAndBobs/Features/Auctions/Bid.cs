using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;

namespace BitsAndBobs.Features.Auctions;

public class Bid : BitsAndBobsTable.Item
{
    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedMember.Global
    public Bid()
    {
    }

    public Bid(AuctionId auction, UserId bidder, decimal amount)
    {
        AuctionId = auction;
        BidId = $"bid#{DateTimeOffset.UtcNow.Ticks}";
        BidderId = bidder;
        BidDate = DateTimeOffset.UtcNow;
        Amount = amount;
    }

    /// <summary>
    /// Gets the ID of the auction this bid is for
    /// </summary>
    [DynamoDBIgnore]
    public AuctionId AuctionId { get; protected set; }

    protected override string PK
    {
        get => AuctionId.Value;
        set => AuctionId = AuctionId.Parse(value);
    }

    /// <summary>
    /// Gets the ID of the bid (only unique within the auction)
    /// </summary>
    [DynamoDBIgnore]
    public string BidId { get; protected set; } = "";

    protected override string SK
    {
        get => BidId;
        set => BidId = value;
    }

    /// <summary>
    /// Gets the user ID of the bidder
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId BidderId { get; protected set; }

    /// <summary>
    /// Gets the date the bid was placed
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset BidDate { get; protected set; }

    /// <summary>
    /// Gets the amount of the bid
    /// </summary>
    public decimal Amount { get; protected set; }
}
