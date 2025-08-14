using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions.Contracts;
using MassTransit;

namespace BitsAndBobs.Features.Auctions.Consumers;

// ReSharper disable once ClassNeverInstantiated.Global
public class AuctionCancelledConsumer(AuctionService auctionService) : IConsumer<AuctionCancelled>
{
    public async Task Consume(ConsumeContext<AuctionCancelled> context)
    {
        var message = context.Message;
        var auction = await auctionService.GetAuction(AuctionId.Parse(message.AuctionId));

        if (auction is null)
            throw new ArgumentException("Auction not found");

        if (auction.CurrentBidderId.HasValue)
            await context.Publish(new SendAuctionCancelledEmailToCurrentBidder(auction.Id.Value));
    }
}
