using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using MassTransit;

namespace BitsAndBobs.Features.Auctions;

public class SendAuctionEmailsConsumer(AuctionService auctionService, UserStore userStore, EmailStore emails)
    : IConsumer<SendAuctionCompletedEmailToSeller>, IConsumer<SendAuctionCompletedEmailToWinner>
{
    public async Task Consume(ConsumeContext<SendAuctionCompletedEmailToSeller> context)
    {
        var auction = await auctionService.GetAuction(AuctionId.Parse(context.Message.AuctionId));

        if (auction is null)
            throw new ArgumentException("Auction not found");

        var seller = await userStore.FindByIdAsync(auction.SellerId.Value, context.CancellationToken);

        if (seller is null)
            throw new ArgumentException("Seller not found");

        if (!auction.CurrentBidderId.HasValue)
        {
            await emails.SendAuctionCompletedWithoutBidToSeller(seller, auction);
        }
        else
        {
            var winner = auction.CurrentBidderId!.Value;
            var names = await userStore.GetDisplayNames(new HashSet<UserId> { winner });
            await emails.SendAuctionCompletedToSeller(seller, auction, names[winner]);
        }
    }

    public async Task Consume(ConsumeContext<SendAuctionCompletedEmailToWinner> context)
    {
        var auction = await auctionService.GetAuction(AuctionId.Parse(context.Message.AuctionId));

        if (auction is null)
            throw new ArgumentException("Auction not found");

        var winner = await userStore.FindByIdAsync(auction.CurrentBidderId!.Value.Value, context.CancellationToken);

        if (winner is null)
            throw new ArgumentException("Winner not found");

        await emails.SendAuctionCompletedToWinner(winner, auction);
    }
}
