using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Email;

public class EmailStore : IEmailSender<User>
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly IDynamoDBContext _context;

    public EmailStore(IAmazonDynamoDB dynamo, IDynamoDBContext context)
    {
        _dynamo = dynamo;
        _context = context;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var query = new Uri(WebUtility.HtmlDecode(confirmationLink)).Query;
        var message = new EmailMessage(
            user,
            email,
            "Email Confirmation",
            $"Please confirm your account by [clicking here](/confirmemail{query})."
        );

        return _context.SaveItem(message);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var query = new Uri(WebUtility.HtmlDecode(resetLink)).Query;
        var message = new EmailMessage(
            user,
            email,
            "Password Reset Link",
            $"Please reset your password by [clicking here](/resetpassword{query})."
        );

        return _context.SaveItem(message);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var message = new EmailMessage(
            user,
            email,
            "Password Reset Link",
            $"Please reset your password by [clicking here](/resetpassword?email={WebUtility.UrlEncode(email)}&code={resetCode})."
        );

        return _context.SaveItem(message);
    }

    /// <summary>
    /// Gets emails recently sent to a specific email address.
    /// </summary>
    public async Task<IEnumerable<EmailMessage>> GetRecentEmails(string emailAddress)
    {
        var query = _context.QueryAsync<EmailMessage>(
            emailAddress.ToUpperInvariant(),
            QueryOperator.GreaterThan,
            [DateTime.UtcNow.AddDays(-1).Ticks],
            new QueryConfig { IndexName = "EmailsByRecipientEmail" }
        );

        return await query.GetRemainingAsync();
    }

    /// <summary>
    /// Gets emails recently sent to a specific user.
    /// </summary>
    public async Task<IEnumerable<EmailMessage>> GetRecentEmails(User user)
    {
        var query = _context.QueryAsync<EmailMessage>(
            user.Id,
            QueryOperator.GreaterThan,
            [DateTime.UtcNow.AddDays(-1).Ticks],
            new QueryConfig { IndexName = "EmailsByRecipientUser" }
        );

        return await query.GetRemainingAsync();
    }

    public Task<bool> SendAuctionCompletedToSeller(User seller, Auction auction, string winnerName)
    {
        var message = new EmailMessage(
            seller,
            seller.EmailAddress,
            $"Auction for '{auction.Name}' ended",
            $"Your auction for [{auction.Name}](/auction/{auction.Id.FriendlyValue}) ended and was won by {winnerName} with a price of ${auction.CurrentPrice:C}. If this were real you'd probably contact them about paying."
        );

        return SendOneTimeEmail(message, new EmailSent(message, "auctioncomplete", auction.Id, seller.Id));
    }

    public Task<bool> SendAuctionCompletedWithoutBidToSeller(User seller, Auction auction)
    {
        var message = new EmailMessage(
            seller,
            seller.EmailAddress,
            $"Auction for '{auction.Name}' ended",
            $"Your auction for [{auction.Name}](/auction/{auction.Id.FriendlyValue}) ended without any bids. Try a more realistic price next time."
        );

        return SendOneTimeEmail(message, new EmailSent(message, "auctioncomplete", auction.Id, seller.Id));
    }

    public Task<bool> SendAuctionCompletedToWinner(User winner, Auction auction)
    {
        var message = new EmailMessage(
            winner,
            winner.EmailAddress,
            $"You won '{auction.Name}'",
            $"Congratulations, you just won [{auction.Name}](/auction/{auction.Id.FriendlyValue}) for only ${auction.CurrentPrice:C}. If this were real you'd probably hear from {auction.SellerDisplayName} about paying."
        );

        return SendOneTimeEmail(message, new EmailSent(message, "auctioncomplete", auction.Id, winner.Id));
    }

    public Task<bool> SendAuctionCancelledToCurrentBidder(User currentBidder, Auction auction)
    {
        var message = new EmailMessage(
            currentBidder,
            currentBidder.EmailAddress,
            $"Auction of '{auction.Name}' cancelled",
            $"{auction.SellerDisplayName} has cancelled the auction of [{auction.Name}](/auction/{auction.Id.FriendlyValue}). Your bidding was a total waste of time."
        );

        return SendOneTimeEmail(message, new EmailSent(message, "auctioncancelled", auction.Id, currentBidder.Id));
    }

    public Task<bool> SendOutbidEmailToHasBeenBidder(User hasBeen, User outbidder, Auction auction, Bid bid)
    {
        var message = new EmailMessage(
            hasBeen,
            hasBeen.EmailAddress,
            $"You were outbid on '{auction.Name}'",
            $"That rascal {outbidder.DisplayName} outbid you on [{auction.Name}](/auction/{auction.Id.FriendlyValue}). The price is now ${auction.CurrentPrice:C}, can you beat that?"
        );

        return SendOneTimeEmail(message, new EmailSent(message, "auctioncancelled", auction.Id, bid.BidId, hasBeen.Id));
    }

    private async Task<bool> SendOneTimeEmail(EmailMessage email, EmailSent sent)
    {
        try
        {
            var items = new List<TransactWriteItem>
            {
                new() { Put = _context.CreateInsertPut(email) },
                new() { Put = _context.CreateInsertPut(sent) },
            };

            await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });

            return true;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return false;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    /// <summary>
    /// This is used to insert records to ensure we only "send" certain emails once.
    /// </summary>
    public class EmailSent : BitsAndBobsTable.Item
    {
        [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
        public EmailSent()
        {
        }

        public EmailSent(EmailMessage message, string type, params object[] ids)
        {
            // ReSharper disable VirtualMemberCallInConstructor
            PK = message.RecipientUserId.Value;
            SK = $"email:{type}:{string.Join(':', ids)}";
            // ReSharper restore VirtualMemberCallInConstructor
            EmailId = message.Id;
        }

        [DynamoDBProperty(typeof(EmailId.DynamoConverter))]
        public EmailId EmailId { get; set; }
    }
}
