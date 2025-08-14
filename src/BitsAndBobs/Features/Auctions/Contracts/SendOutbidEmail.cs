namespace BitsAndBobs.Features.Auctions.Contracts;

public record SendOutbidEmail(string AuctionId, string BidId, string UserId, string OutbidderUserId);
