namespace BitsAndBobs.Contracts;

public sealed record BidAccepted(string BidId, string AuctionId, string UserId, string? PreviousCurrentBidderUserId, string CurrentBidderUserId);
