using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Consumers;
using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using Microsoft.Testing.Platform.Services;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Consumers;

[TestFixture]
public class SendAuctionEmailsConsumerTest : AuctionTestBase
{
    [Test]
    public async Task ShouldSendAuctionCompletedWithNoBidToSeller()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser();
        var auction = await CreateAuction(seller, endDate: DateTimeOffset.Now.AddMinutes(-1), configure: a => a.Complete());

        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToSeller(auction.Id.Value));
        await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCompletedEmailToSeller>();

        var emails = await GetEmails(seller);
        emails.Count.ShouldBe(1);
        emails[0].RecipientEmail.ShouldBe(seller.EmailAddress);
        emails[0].Type.ShouldBe($"Auction for '{auction.Name}' ended");
        emails[0].Body.ShouldContain($"[{auction.Name}](/auction/{auction.Id.FriendlyValue})");
        emails[0].Body.ShouldContain("Try a more realistic price next time.");
    }

    [Test]
    public async Task ShouldSendAuctionCompletedWithWinnerNameToSeller()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser();
        var winner = await CreateUser(displayName: "Harold Bishop");
        var auction = await CreateAuction(seller, initialPrice: 483.21m);
        await AddBidToAuction(auction, winner.Id, 789m);
        await UpdateStatus(auction, AuctionStatus.Complete, DateTimeOffset.Now.AddDays(-1));

        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToSeller(auction.Id.Value));
        await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCompletedEmailToSeller>();

        var emails = await GetEmails(seller);
        emails.Count.ShouldBe(1);
        emails[0].RecipientEmail.ShouldBe(seller.EmailAddress);
        emails[0].Type.ShouldBe($"Auction for '{auction.Name}' ended");
        emails[0].Body.ShouldContain($"[{auction.Name}](/auction/{auction.Id.FriendlyValue})");
        emails[0].Body.ShouldContain($"won by {winner.DisplayName}");
        emails[0].Body.ShouldContain($"${auction.CurrentPrice}");
    }

    [Test]
    public async Task ShouldNotResendAuctionCompletedToSeller()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser();
        var auction = await CreateAuction(seller, endDate: DateTimeOffset.Now.AddMinutes(-1), configure: a => a.Complete());

        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToSeller(auction.Id.Value));
        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToSeller(auction.Id.Value));
        await WaitForMessagesToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCompletedEmailToSeller>(2);

        var emails = await GetEmails(seller);
        emails.Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldSendAuctionCompletedToWinner()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser(displayName: "Maude");
        var winner = await CreateUser();
        var auction = await CreateAuction(seller, initialPrice: 107.21m);
        await AddBidToAuction(auction, winner.Id, 789m);
        await UpdateStatus(auction, AuctionStatus.Complete, DateTimeOffset.Now.AddDays(-1));

        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToWinner(auction.Id.Value));
        await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCompletedEmailToWinner>();

        var emails = await GetEmails(winner);
        emails.Count.ShouldBe(1);
        emails[0].RecipientEmail.ShouldBe(winner.EmailAddress);
        emails[0].Type.ShouldBe($"You won '{auction.Name}'");
        emails[0].Body.ShouldContain($"[{auction.Name}](/auction/{auction.Id.FriendlyValue})");
        emails[0].Body.ShouldContain($"hear from {auction.SellerDisplayName} ");
        emails[0].Body.ShouldContain($"${auction.CurrentPrice}");
    }

    [Test]
    public async Task ShouldNotResendAuctionCompletedToWinner()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser();
        var winner = await CreateUser();
        var auction = await CreateAuction(seller, initialPrice: 107.21m);
        await AddBidToAuction(auction, winner.Id, 789m);
        await UpdateStatus(auction, AuctionStatus.Complete, DateTimeOffset.Now.AddDays(-1));

        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToWinner(auction.Id.Value));
        await Messaging.Bus.Publish(new SendAuctionCompletedEmailToWinner(auction.Id.Value));
        await WaitForMessagesToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCompletedEmailToWinner>(2);

        var emails = await GetEmails(winner);
        emails.Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldSendAuctionCancelledToCurrentBidder()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser(displayName: "Mildred");
        var currentBidder = await CreateUser();
        var auction = await CreateAuction(seller, initialPrice: 107.21m);
        await AddBidToAuction(auction, currentBidder.Id, 789m);
        await UpdateStatus(auction, AuctionStatus.Cancelled, auction.EndDate);

        await Messaging.Bus.Publish(new SendAuctionCancelledEmailToCurrentBidder(auction.Id.Value));
        await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCancelledEmailToCurrentBidder>();

        var emails = await GetEmails(currentBidder);
        emails.Count.ShouldBe(1);
        emails[0].RecipientEmail.ShouldBe(currentBidder.EmailAddress);
        emails[0].Type.ShouldBe($"Auction of '{auction.Name}' cancelled");
        emails[0].Body.ShouldContain($"[{auction.Name}](/auction/{auction.Id.FriendlyValue})");
        emails[0].Body.ShouldContain($"{auction.SellerDisplayName} has cancelled");
    }

    [Test]
    public async Task ShouldNotResendAuctionCancelledEmail()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var seller = await CreateUser();
        var currentBidder = await CreateUser();
        var auction = await CreateAuction(seller, initialPrice: 107.21m);
        await AddBidToAuction(auction, currentBidder.Id, 789m);
        await UpdateStatus(auction, AuctionStatus.Cancelled, auction.EndDate);

        await Messaging.Bus.Publish(new SendAuctionCancelledEmailToCurrentBidder(auction.Id.Value));
        await Messaging.Bus.Publish(new SendAuctionCancelledEmailToCurrentBidder(auction.Id.Value));
        await WaitForMessagesToBeConsumed<SendAuctionEmailsConsumer, SendAuctionCancelledEmailToCurrentBidder>(2);

        var emails = await GetEmails(currentBidder);
        emails.Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldSendOutbidEmailWithNameOfOutbidderAndCurrentPrice()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var bidder = await CreateUser(displayName: "Sport");
        var outBidder = await CreateUser(displayName: "Tiger");
        var currentBidder = await CreateUser(displayName: "Boss");
        var auction = await CreateAuction(initialPrice: 107.21m);
        await AddBidToAuction(auction, bidder.Id, 110m);
        var bid = await AddBidToAuction(auction, outBidder.Id, 150m);
        await AddBidToAuction(auction, currentBidder.Id, 200m);

        await Messaging.Bus.Publish(new SendOutbidEmail(auction.Id.Value, bid.BidId, bidder.Id.Value, outBidder.Id.Value));
        await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendOutbidEmail>();

        var emails = await GetEmails(bidder);
        emails.Count.ShouldBe(1);
        emails[0].RecipientEmail.ShouldBe(bidder.EmailAddress);
        emails[0].Type.ShouldBe($"You were outbid on '{auction.Name}'");
        emails[0].Body.ShouldContain($"[{auction.Name}](/auction/{auction.Id.FriendlyValue})");
        emails[0].Body.ShouldContain($"That rascal {outBidder.DisplayName}");
        emails[0].Body.ShouldContain($"{auction.CurrentPrice:C}");
    }

    [Test]
    public async Task ShouldNotSendOutbidEmailWhenBidNotFound()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var bidder = await CreateUser(displayName: "Sport");
        var outBidder = await CreateUser(displayName: "Tiger");
        var auction = await CreateAuction(initialPrice: 107.21m);
        await AddBidToAuction(auction, bidder.Id, 110m);

        await Messaging.Bus.Publish(new SendOutbidEmail(auction.Id.Value, "bid#not-found", bidder.Id.Value, outBidder.Id.Value));

        (await WaitForMessageToBeConsumed<SendAuctionEmailsConsumer, SendOutbidEmail>(x => x.Exception != null)).ShouldBeTrue();
        var emails = await GetEmails(bidder);
        emails.Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldNotResentOutbidEmail()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var bidder = await CreateUser(displayName: "Sport");
        var outBidder = await CreateUser(displayName: "Tiger");
        var auction = await CreateAuction(initialPrice: 107.21m);
        await AddBidToAuction(auction, bidder.Id, 110m);
        var bid = await AddBidToAuction(auction, outBidder.Id, 150m);

        await Messaging.Bus.Publish(new SendOutbidEmail(auction.Id.Value, bid.BidId, bidder.Id.Value, outBidder.Id.Value));
        await WaitForMessagesToBeConsumed<SendAuctionEmailsConsumer, SendOutbidEmail>(2);

        var emails = await GetEmails(bidder);
        emails.Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldSendSeparateOutbidEmailsToOneUserForDifferentBidsOnSameAuction()
    {
        ConfigureMessaging(c => c.AddConsumer<SendAuctionEmailsConsumer>());
        var bidder = await CreateUser(displayName: "Sport");
        var outBidder = await CreateUser(displayName: "Tiger");
        var auction = await CreateAuction(initialPrice: 107.21m);
        await AddBidToAuction(auction, bidder.Id, 110m);
        var bid1 = await AddBidToAuction(auction, outBidder.Id, 150m);
        await AddBidToAuction(auction, bidder.Id, 200m);
        var bid2 = await AddBidToAuction(auction, outBidder.Id, 250m);

        await Messaging.Bus.Publish(new SendOutbidEmail(auction.Id.Value, bid1.BidId, bidder.Id.Value, outBidder.Id.Value));
        await Messaging.Bus.Publish(new SendOutbidEmail(auction.Id.Value, bid2.BidId, bidder.Id.Value, outBidder.Id.Value));
        await WaitForMessagesToBeConsumed<SendAuctionEmailsConsumer, SendOutbidEmail>(2);

        var emails = await GetEmails(bidder);
        emails.Count.ShouldBe(2);
    }

    private async Task<List<EmailMessage>> GetEmails(User user) =>
        (await AppFactory.Services.GetRequiredService<EmailStore>().GetRecentEmails(user)).ToList();
}
