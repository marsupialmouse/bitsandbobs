namespace BitsAndBobs.Features.Auctions;

public enum AuctionStatus
{
    /// <summary>
    /// The auction is open for bidding
    /// </summary>
    Open,

    /// <summary>
    /// The auction has ended
    /// </summary>
    Ended,

    /// <summary>
    /// The auction has been cancelled
    /// </summary>
    Cancelled,
}
