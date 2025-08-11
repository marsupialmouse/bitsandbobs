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

public class Auction : BitsAndBobsTable.VersionedEntity
{
    public const string SortKey = "Auction";

    private Bid? _currentBid;
    private List<Bid> _bids = [];

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
        EndDate = DateTimeOffset.UtcNow.Add(period);
        SellerId = seller.Id;
        SellerDisplayName = seller.DisplayName;

        image.AssociateWithAuction(this);

        UpdateVersion();
    }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBIgnore]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global (DynamoDB)
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public AuctionId Id { get; protected set; }

    protected override string PK
    {
        get => Id.Value;
        set => Id = AuctionId.Parse(value);
    }

    // ReSharper disable once UnusedMember.Global (DynamoDB)
    // ReSharper disable once InconsistentNaming
    protected override string SK
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
    public DateTimeOffset CreatedDate { get; protected set; } = DateTimeOffset.UtcNow;

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

    /// <summary>
    /// Gets the date the auction was cancelled, if it is cancelled
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset? CancelledDate { get; protected set; }

    /// <summary>
    /// Gets the current auction status
    /// </summary>
    [DynamoDBProperty("AuctionStatus")]
    public AuctionStatus Status { get; protected set; } = AuctionStatus.Open;

    /// <summary>
    /// Whether the auction is currently open for bidding
    /// </summary>
    public bool IsOpen => Status == AuctionStatus.Open && EndDate > DateTimeOffset.Now;

    /// <summary>
    ///  Whether the auction is closed to more bids.
    /// </summary>
    public bool IsClosed => Status != AuctionStatus.Cancelled && !IsOpen;

    /// <summary>
    /// Whether the auction has been cancelled
    /// </summary>
    public bool IsCancelled => Status == AuctionStatus.Cancelled;

    /// <summary>
    /// Gets the user ID of the seller
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId SellerId { get; protected set; }

    /// <summary>
    /// Gets the seller's display name
    /// </summary>
    public string SellerDisplayName { get; protected set; } = "";

    /// <summary>
    /// Gets the current price of the lot
    /// </summary>
    public decimal CurrentPrice { get; protected set; }

    /// <summary>
    /// Gets the minimum bid required to take part in the auction
    /// </summary>
    public decimal MinimumBid => HasBid ? CurrentPrice + BidIncrement : InitialPrice;

    /// <summary>
    /// Whether there are any bids
    /// </summary>
    private bool HasBid => NumberOfBids > 0;

    /// <summary>
    /// Gets the list of bids that have been placed on this auction.
    /// </summary>
    [DynamoDBIgnore]
    public IReadOnlyCollection<Bid> Bids
    {
        get => _bids;
        internal set
        {
            if (_bids.Count > 0 || NumberOfBids == 0 && value.Count > 0)
                throw new InvalidOperationException("Bids should only be set when retrieving the auction");
            _bids = value.OrderBy(x => x.BidDate).ToList();
        }
    }

    /// <summary>
    /// Gets the current bid. Note that this only works if bids have been loaded.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public Bid? CurrentBid
    {
        get
        {
            if (_currentBid == null && CurrentBidId != null)
            {
                if (Bids.Count == 0)
                    throw new ArgumentException("No bids have been set.");

                _currentBid = Bids.FirstOrDefault(b => b.BidId == CurrentBidId);
            }

            return _currentBid;
        }
        private set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            _currentBid = value;
            CurrentBidId = value.BidId;
            CurrentBidderId = value.BidderId;
        }
    }

    /// <summary>
    /// Gets the ID of the current winning bid
    /// </summary>
    public string? CurrentBidId { get; protected set; }

    /// <summary>
    /// Gets ID of the current winning bidder
    /// </summary>
    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId? CurrentBidderId { get; protected set; }

    /// <summary>
    /// Gets the number of bids that have been placed on this auction
    /// </summary>
    public int NumberOfBids { get; protected set; }

    /// <summary>
    /// Cancels an open auction.
    /// </summary>
    public void Cancel()
    {
        if (!IsOpen)
            throw new InvalidAuctionStateException("Cannot cancel an auction that is not open.");

        Status = AuctionStatus.Cancelled;
        CancelledDate = DateTimeOffset.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Marks a finished auction as complete.
    /// </summary>
    public void Complete()
    {
        if (Status != AuctionStatus.Open)
            throw new InvalidAuctionStateException("Cannot complete an auction that is not open.");

        if (EndDate > DateTimeOffset.Now)
            throw new InvalidAuctionStateException("Cannot complete an auction that has not ended.");

        Status = AuctionStatus.Complete;
        UpdateVersion();
    }

    public Bid AddBid(UserId bidder, decimal amount)
    {
        if (bidder == SellerId)
            throw new InvalidOperationException("Seller cannot bid on their own auction.");

        if (!IsOpen)
            throw new InvalidAuctionStateException("Cannot add a bid to an auction that is not open.");

        if (amount < MinimumBid)
            throw new InvalidAuctionStateException($"Bid amount must be at least {MinimumBid}.");

        var isCurrentBidder = bidder == CurrentBidderId;

        // The current bidder can add a new bid to increase their limit without affecting the current price
        if (isCurrentBidder && amount <= CurrentBid!.Amount)
            throw new InvalidAuctionStateException("Cannot place a bid that is not higher than the current bid.");

        var bid = new Bid(Id, bidder, amount);

        if (!HasBid || isCurrentBidder)
        {
            CurrentBid = bid;
        }
        else if (amount > CurrentBid!.Amount)
        {
            CurrentPrice = Math.Min(amount, CurrentBid.Amount + BidIncrement);
            CurrentBid = bid;
        }
        else
        {
            CurrentPrice = Math.Min(CurrentBid.Amount, amount + BidIncrement);
        }

        _bids.Add(bid);
        NumberOfBids++;
        UpdateVersion();

        return bid;
    }
}
