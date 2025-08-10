using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

[TestFixture]
public class AuctionAddBidTests
{
    [Test]
    public void ShouldThrowExceptionIfSellerBidsOnTheirOwnAuction()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var exception = Should.Throw<InvalidOperationException>(() => auction.AddBid(auction.SellerId, 110m));

        exception.Message.ShouldBe("Seller cannot bid on their own auction.");
    }

    [Test]
    public void ShouldBeInvalidBidWhenAuctionIsFinished()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m, period: TimeSpan.FromMinutes(-10));

        var exception = Should.Throw<InvalidAuctionStateException>(() => auction.AddBid(UserId.Create(), 110m));

        exception.Message.ShouldBe("Cannot add a bid to an auction that is not open.");
    }

    [Test]
    public void ShouldBeInvalidBidWhenAuctionIsCancelled()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        auction.Cancel();

        var exception = Should.Throw<InvalidAuctionStateException>(() => auction.AddBid(UserId.Create(), 110m));

        exception.Message.ShouldBe("Cannot add a bid to an auction that is not open.");
    }

    [Test]
    public void ShouldBeInvalidBidIfBidLessThanMinimumBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var exception = Should.Throw<InvalidAuctionStateException>(() => auction.AddBid(UserId.Create(), 99m));

        exception.Message.ShouldBe("Bid amount must be at least 100.");
    }

    [Test]
    public void ShouldCreateBidWithCorrectValues()
    {
        var bidder = UserId.Create();
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var bid = auction.AddBid(bidder, 150m);

        bid.ShouldNotBeNull();
        bid.BidId.ShouldNotBeNullOrEmpty();
        bid.AuctionId.ShouldBe(auction.Id);
        bid.BidderId.ShouldBe(bidder);
        bid.Amount.ShouldBe(150m);
        bid.BidDate.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void ShouldMaintainInitialPriceForInitialBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var bid = auction.AddBid(UserId.Create(), 120m);

        bid.ShouldNotBeNull();
        auction.CurrentBidId.ShouldBe(bid.BidId);
        auction.CurrentBidderId.ShouldBe(bid.BidderId);
        auction.CurrentPrice.ShouldBe(100m);
        auction.NumberOfBids.ShouldBe(1);
        auction.Bids.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldIncreasePriceByIncrementAndUpdateCurrentBidForHigherBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        auction.AddBid(UserId.Create(), 120m);

        var secondBid = auction.AddBid(UserId.Create(), 150m);

        auction.CurrentBidId.ShouldBe(secondBid.BidId);
        auction.CurrentBidderId.ShouldBe(secondBid.BidderId);
        auction.CurrentPrice.ShouldBe(130m);
        auction.NumberOfBids.ShouldBe(2);
        auction.Bids.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldIncreasePriceAndMaintainCurrentBidForLowerMaximumBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        var firstBid = auction.AddBid(UserId.Create(), 150m);

        auction.AddBid(UserId.Create(), 120m);

        auction.CurrentBidId.ShouldBe(firstBid.BidId);
        auction.CurrentBidderId.ShouldBe(firstBid.BidderId);
        auction.CurrentPrice.ShouldBe(130m);
        auction.NumberOfBids.ShouldBe(2);
    }

    [Test]
    public void ShouldAddNewBidAndLeavePriceUnchangedWhenCurrentBidderAddsBid()
    {
        var bidder = UserId.Create();
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        auction.AddBid(bidder, 120m);

        var bid = auction.AddBid(bidder, 200m);

        auction.CurrentBidId.ShouldBe(bid.BidId);
        auction.CurrentBidderId.ShouldBe(bid.BidderId);
        auction.CurrentPrice.ShouldBe(100m);
        auction.NumberOfBids.ShouldBe(2);
        auction.Bids.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldBeInvalidBidWhenCurrentBidderBidsNoMoreThanLastBid()
    {
        var bidder = UserId.Create();
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        auction.AddBid(bidder, 120m);

        var exception = Should.Throw<InvalidAuctionStateException>(() => auction.AddBid(bidder, 120m));

        exception.Message.ShouldBe("Cannot place a bid that is not higher than the current bid.");
    }

    [Test]
    public void ShouldAllowBidAtInitialPriceWhenAuctionHasNoBids()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var bid = auction.AddBid(UserId.Create(), 100m);

        bid.ShouldNotBeNull();
        auction.CurrentPrice.ShouldBe(100m);
        auction.CurrentBidId.ShouldBe(bid.BidId);
        auction.CurrentBidderId.ShouldBe(bid.BidderId);
        auction.NumberOfBids.ShouldBe(1);
    }

    [Test]
    public void ShouldKeepCurrentBidAndUpdatePriceWhenNewBidMatchesCurrentMaximumBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);

        var firstBid = auction.AddBid(UserId.Create(), 155m);
        var secondBid = auction.AddBid(UserId.Create(), 155m);

        secondBid.ShouldNotBeNull();
        auction.CurrentBidId.ShouldBe(firstBid.BidId);
        auction.CurrentBidderId.ShouldBe(firstBid.BidderId);
        auction.CurrentPrice.ShouldBe(155m);
        auction.NumberOfBids.ShouldBe(2);
        auction.Bids.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldIncreasePriceOverCurrentMaximumBidByLessThanIncrementWhenIncrementGreaterThanMaximumBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 12m);

        auction.AddBid(UserId.Create(), 136m);
        var secondBid = auction.AddBid(UserId.Create(), 147m);

        secondBid.ShouldNotBeNull();
        auction.CurrentBidId.ShouldBe(secondBid.BidId);
        auction.CurrentBidderId.ShouldBe(secondBid.BidderId);
        auction.CurrentPrice.ShouldBe(147m);
        auction.NumberOfBids.ShouldBe(2);
        auction.Bids.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldUpdateVersionAfterSuccessfulBid()
    {
        var auction = CreateAuction(initialPrice: 100m, bidIncrement: 10m);
        var initialVersion = auction.Version;

        auction.AddBid(UserId.Create(), 110m);
        var versionAfterFirstBid = auction.Version;
        auction.AddBid(UserId.Create(), 110m);
        var versionAfterSecondBid = auction.Version;

        versionAfterFirstBid.ShouldNotBe(initialVersion);
        versionAfterSecondBid.ShouldNotBe(initialVersion);
        versionAfterSecondBid.ShouldNotBe(versionAfterFirstBid);
    }

    private static Auction CreateAuction(decimal initialPrice, decimal bidIncrement, TimeSpan? period = null)
    {
        var seller = new User { DisplayName = "Jimmy Sharman" };
        var auctionImage = new AuctionImage(".jpg", seller.Id);

        return new Auction(
            seller,
            "Test Auction",
            "Test Description",
            auctionImage,
            initialPrice,
            bidIncrement,
            period ?? TimeSpan.FromHours(1)
        );
    }
}
