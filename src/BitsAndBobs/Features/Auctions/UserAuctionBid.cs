using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;

namespace BitsAndBobs.Features.Auctions;

/// <summary>
/// Keeps track of the user's most recent bids
/// </summary>
public class UserAuctionBid : BitsAndBobsTable.Item
{
    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedMember.Global
    public UserAuctionBid()
    {
    }

    public UserAuctionBid(Bid bid)
    {
        UserId = bid.BidderId;
        AuctionId = bid.AuctionId;
        Amount = bid.Amount;
        LastBidDate = bid.BidDate;
    }

    /// <summary>
    /// Gets the ID of the user
    /// </summary>
    [DynamoDBIgnore]
    public UserId UserId { get; protected set; }

    protected override string PK
    {
        get => UserId.Value;
        set => UserId = UserId.Parse(value);
    }

    /// <summary>
    /// Gets the ID of the auction the user bid on
    /// </summary>
    [DynamoDBIgnore]
    public AuctionId AuctionId { get; protected set; }

    protected override string SK
    {
        get => AuctionId.Value;
        set => AuctionId = AuctionId.Parse(value);
    }

    /// <summary>
    /// Gets the user's maximum bid amount for the auction
    /// </summary>
    public decimal Amount { get; protected set;  }

    /// <summary>
    /// Gets the date of the user's last bid on the auction
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset LastBidDate { get; protected set; }

    // This property forms part of a GSI
    // ReSharper disable once UnusedMember.Global
    protected long LastUserBidUtcTimestamp
    {
        get => LastBidDate.UtcTicks;
        // ReSharper disable once ValueParameterNotUsed
        set { }
    }
}
