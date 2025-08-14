using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Consumers;

public class BidAcceptedConsumerTest : AuctionTestBase
{
    [Test]
    public async Task ShouldPublishOutbidEmailCommandForPreviousCurrentBidderWhenAcceptedBidIsNewCurrentBid()
    {
        var auctionId = AuctionId.Create().Value;
        var bidderId = UserId.Create().Value;
        var previousBidderId = UserId.Create().Value;

        await Messaging.Bus.Publish(
            new BidAccepted(
                AuctionId: auctionId,
                BidId: "bid#38",
                UserId: bidderId,
                PreviousCurrentBidderUserId: previousBidderId,
                CurrentBidderUserId: bidderId
            )
        );

        var message = await GetPublishedMessage<SendOutbidEmail>();
        message.ShouldNotBeNull();
        message.AuctionId.ShouldBe(auctionId);
        message.BidId.ShouldBe("bid#38");
        message.UserId.ShouldBe(previousBidderId);
        message.OutbidderUserId.ShouldBe(bidderId);
    }

    [Test]
    public async Task ShouldPublishOutbidEmailCommandForBidderWhenAcceptedBidNotHighestBid()
    {
        var auctionId = AuctionId.Create().Value;
        var bidderId = UserId.Create().Value;
        var previousBidderId = UserId.Create().Value;

        await Messaging.Bus.Publish(
            new BidAccepted(
                AuctionId: auctionId,
                BidId: "bid#1",
                UserId: bidderId,
                PreviousCurrentBidderUserId: previousBidderId,
                CurrentBidderUserId: previousBidderId
            )
        );

        var message = await GetPublishedMessage<SendOutbidEmail>();
        message.ShouldNotBeNull();
        message.AuctionId.ShouldBe(auctionId);
        message.BidId.ShouldBe("bid#1");
        message.UserId.ShouldBe(bidderId);
        message.OutbidderUserId.ShouldBe(previousBidderId);
    }

    [Test]
    public async Task ShouldNotPublishOutbidEmailCommandWhenThereWasNoPreviousBid()
    {
        var auctionId = AuctionId.Create().Value;
        var bidderId = UserId.Create().Value;

        await Messaging.Bus.Publish(
            new BidAccepted(
                AuctionId: auctionId,
                BidId: "bid#11",
                UserId: bidderId,
                PreviousCurrentBidderUserId: null,
                CurrentBidderUserId: bidderId
            )
        );

        (await Messaging.Published.Any<SendOutbidEmail>()).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldNotPublishOutbidEmailCommandWhenCurrentBidderIncreasesBid()
    {
        var auctionId = AuctionId.Create().Value;
        var bidderId = UserId.Create().Value;

        await Messaging.Bus.Publish(
            new BidAccepted(
                AuctionId: auctionId,
                BidId: "bid#1",
                UserId: bidderId,
                PreviousCurrentBidderUserId: bidderId,
                CurrentBidderUserId: bidderId
            )
        );

        (await Messaging.Published.Any<SendOutbidEmail>()).ShouldBeFalse();
    }
}
