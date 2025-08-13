using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions.Contracts;
using MassTransit;

namespace BitsAndBobs.Features.Auctions;

public class AuctionCompletedConsumer(AuctionService auctionService) : IConsumer<AuctionCompleted>
{
    public async Task Consume(ConsumeContext<AuctionCompleted> context)
    {
        var message = context.Message;
        var auction = await auctionService.GetAuction(AuctionId.Parse(message.AuctionId));

        if (auction is null)
            throw new ArgumentException("Auction not found");

        await context.Publish(new SendAuctionCompletedEmailToSeller(message.AuctionId));

        if (auction.CurrentBidderId.HasValue)
            await context.Publish(new SendAuctionCompletedEmailToWinner(message.AuctionId));
    }
}
