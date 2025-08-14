using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Consumers;
using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Consumers;

public class AuctionCancelledConsumerTest : AuctionTestBase
{
    [Test]
    public async Task ShouldPublishCommandWhenAuctionHasCurrentBidder()
    {
        ConfigureMessaging(c => c.AddConsumer<AuctionCancelledConsumer>());
        var auction = await CreateAuction();
        await AddBidToAuction(auction, UserId.Create(), 799);
        await UpdateStatus(auction, AuctionStatus.Cancelled, auction.EndDate);

        await Messaging.Bus.Publish(new AuctionCancelled(auction.Id.Value));

        (await Messaging.Published.Any<SendAuctionCancelledEmailToCurrentBidder>()).ShouldBeTrue();
    }

    [Test]
    public async Task ShouldNotPublishCommandWhenAuctionHasNoBids()
    {
        ConfigureMessaging(c => c.AddConsumer<AuctionCancelledConsumer>());
        var auction = await CreateAuction(configure: a => a.Cancel());

        await Messaging.Bus.Publish(new AuctionCancelled(auction.Id.Value));

        (await Messaging.Published.Any<SendAuctionCancelledEmailToCurrentBidder>()).ShouldBeFalse();
    }
}
