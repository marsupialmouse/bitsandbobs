using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Contracts;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using Microsoft.Testing.Platform.Services;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions;

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
        var winner = await CreateUser(u => u.DisplayName = "Harold Bishop");
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
        var seller = await CreateUser(u => u.DisplayName = "Maude");
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

    private async Task<List<EmailMessage>> GetEmails(User user) =>
        (await AppFactory.Services.GetRequiredService<EmailStore>().GetRecentEmails(user)).ToList();
}
