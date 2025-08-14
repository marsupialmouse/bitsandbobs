namespace BitsAndBobs.Features.Auctions.Contracts;

public record SendAuctionCompletedEmailToSeller(string AuctionId);

public record SendAuctionCompletedEmailToWinner(string AuctionId);

public record SendAuctionCancelledEmailToCurrentBidder(string AuctionId);
