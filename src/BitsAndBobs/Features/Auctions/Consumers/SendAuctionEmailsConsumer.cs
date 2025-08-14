using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using MassTransit;

namespace BitsAndBobs.Features.Auctions.Consumers;

// ReSharper disable once ClassNeverInstantiated.Global
public class SendAuctionEmailsConsumer(AuctionService auctionService, UserStore userStore, EmailStore emails)
    : IConsumer<SendAuctionCompletedEmailToSeller>, IConsumer<SendAuctionCompletedEmailToWinner>, IConsumer<SendAuctionCancelledEmailToCurrentBidder>
{
    public async Task Consume(ConsumeContext<SendAuctionCompletedEmailToSeller> context)
    {
        var auction = await GetAuction(context.Message.AuctionId);
        var seller = await GetUser(auction.SellerId, context.CancellationToken);

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
        var auction = await GetAuction(context.Message.AuctionId);
        var winner = await GetUser(auction.CurrentBidderId!.Value, context.CancellationToken);

        await emails.SendAuctionCompletedToWinner(winner, auction);
    }

    public async Task Consume(ConsumeContext<SendAuctionCancelledEmailToCurrentBidder> context)
    {
        var auction = await GetAuction(context.Message.AuctionId);
        var currentBidder = await GetUser(auction.CurrentBidderId!.Value, context.CancellationToken);

        await emails.SendAuctionCancelledToCurrentBidder(currentBidder, auction);
    }

    private async Task<Auction> GetAuction(string auctionId) =>
        (await auctionService.GetAuction(AuctionId.Parse(auctionId)))
        ?? throw new ArgumentException("Auction not found");

    private async Task<User> GetUser(UserId userId, CancellationToken cancellationToken) =>
        (await userStore.FindByIdAsync(userId.Value, cancellationToken))
        ?? throw new ArgumentException("User not found");
}
