using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions.Contracts;
using MassTransit;

namespace BitsAndBobs.Features.Auctions.Consumers;

public class BidAcceptedConsumer : IConsumer<BidAccepted>
{
    public async Task Consume(ConsumeContext<BidAccepted> context)
    {
        var message = context.Message;

        if (message.PreviousCurrentBidderUserId is not null
            && (message.CurrentBidderUserId != message.PreviousCurrentBidderUserId
                || message.UserId != message.CurrentBidderUserId))
        {
            var loser = message.UserId == message.CurrentBidderUserId
                            ? message.PreviousCurrentBidderUserId
                            : message.UserId;

            await context.Publish(
                new SendOutbidEmail(
                    AuctionId: message.AuctionId,
                    BidId: message.BidId,
                    UserId: loser,
                    OutbidderUserId: message.CurrentBidderUserId
                )
            );
        }
    }
}
