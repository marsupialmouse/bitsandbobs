using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Consumers;
using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Consumers;

[TestFixture]
public class AuctionCompletedConsumerTest : AuctionTestBase
{
    [Test]
    public async Task ShouldPublishOneEmailCommandWhenAuctionHasNoBids()
    {
        ConfigureMessaging(c => c.AddConsumer<AuctionCompletedConsumer>());
        var auction = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(-1), configure: a => a.Complete());

        await Messaging.Bus.Publish(new AuctionCompleted(auction.Id.Value));

        (await Messaging.Published.Any<SendAuctionCompletedEmailToSeller>()).ShouldBeTrue();
        (await Messaging.Published.Any<SendAuctionCompletedEmailToWinner>()).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldPublishTwoEmailCommandsWhenAuctionHasWinner()
    {
        ConfigureMessaging(c => c.AddConsumer<AuctionCompletedConsumer>());
        var auction = await CreateAuction();
        await AddBidToAuction(auction, UserId.Create(), 599m);
        await UpdateStatus(auction, AuctionStatus.Complete, DateTimeOffset.Now.AddMinutes(-10));

        await Messaging.Bus.Publish(new AuctionCompleted(auction.Id.Value));

        (await Messaging.Published.Any<SendAuctionCompletedEmailToSeller>()).ShouldBeTrue();
        (await Messaging.Published.Any<SendAuctionCompletedEmailToWinner>()).ShouldBeTrue();
    }
}
