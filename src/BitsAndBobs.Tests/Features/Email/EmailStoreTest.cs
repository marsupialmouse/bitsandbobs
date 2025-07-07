using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Email;

public class EmailStoreTest
{
    [Test]
    public async Task ShouldSaveEmailConfirmation()
    {
        const string emailAddress = "email-confirm@fufme.com";
        var user = new User { EmailAddress = "user-confirm@fufme.com" };
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendConfirmationLinkAsync(user, emailAddress, "https://example.com/confirm/path?token=123");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.SK.ShouldBe(email.SentAt.UtcDateTime.ToString("O"));
        email.UserId.ShouldBe(user.Id);
        email.Recipient.ShouldBe(emailAddress);
        email.Type.ShouldBe("Email Confirmation");
        email.SentAt.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldReplaceEmailConfirmationLinkWithPathAndQuery()
    {
        const string emailAddress = "email-confirm-link@fufme.com";
        var user = new User();
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendConfirmationLinkAsync(user, emailAddress, "https://example.com/confirm/path?token=123");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.Body.ShouldContain("<a href='/confirmemail?token=123'>clicking here</a>");
    }

    [Test]
    public async Task ShouldSavePasswordResetLink()
    {
        const string emailAddress = "email-resetlink@fufme.com";
        var user = new User { EmailAddress = "user-resetlink@fufme.com" };
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendPasswordResetLinkAsync(user, emailAddress, "https://example.com/reset/path?token=321");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.SK.ShouldBe(email.SentAt.UtcDateTime.ToString("O"));
        email.UserId.ShouldBe(user.Id);
        email.Recipient.ShouldBe(emailAddress);
        email.Type.ShouldBe("Password Reset Link");
        email.SentAt.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldReplacePasswordResetLinkWithPathAndQuery()
    {
        const string emailAddress = "password-resetlink-link@fufme.com";
        var user = new User();
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendPasswordResetLinkAsync(user, emailAddress, "https://example.com/reset/path?token=321");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.Body.ShouldContain($"<a href='/resetpassword?token=321'>clicking here</a>");
    }

    [Test]
    public async Task ShouldSavePasswordResetCode()
    {
        const string emailAddress = "email-resetcode@fufme.com";
        var user = new User { EmailAddress = "user-resetcode@fufme.com" };
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendPasswordResetCodeAsync(user, emailAddress, "parsnip");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.SK.ShouldBe(email.SentAt.UtcDateTime.ToString("O"));
        email.UserId.ShouldBe(user.Id);
        email.Recipient.ShouldBe(emailAddress);
        email.Type.ShouldBe("Password Reset Code");
        email.Body.ShouldContain($"Please reset your password using the following code: parsnip");
        email.SentAt.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldGetRecentEmailsByRecipient()
    {
        var users = new[]
        {
            new User { EmailAddress = "recent-e-1@fufume.com" },
            new User { EmailAddress = "recent-e-2@fufume.com" },
        };
        var emails = new[]
        {
            CreateEmail(users[0], users[0].EmailAddress, 10),
            CreateEmail(users[0], users[0].EmailAddress, 89),
            CreateEmail(users[0], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 65),
            CreateEmail(users[1], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 13),
        };
        await SaveEmails(emails);
        var emailStore = new EmailStore(Testing.DynamoContext);

        var recentEmails = (await emailStore.GetRecentEmails(users[0].EmailAddress))
            .OrderByDescending(email => email.SentAt)
            .Select(email => (email.PK, email.SK));

        var expectedEmails = emails
            .Where(email => email.Recipient == users[0].EmailAddress && email.SentAt >= DateTimeOffset.Now.AddMinutes(-30))
            .OrderByDescending(email => email.SentAt)
            .Select(email => (email.PK, email.SK));
        recentEmails.ShouldBe(expectedEmails);
    }

    [Test]
    public async Task ShouldGetRecentEmailsByUser()
    {
        var users = new[]
        {
            new User { EmailAddress = "recent-u-1@fufume.com" },
            new User { EmailAddress = "recent-u-2@fufume.com" },
        };
        var emails = new[]
        {
            CreateEmail(users[0], users[0].EmailAddress, 10),
            CreateEmail(users[0], users[0].EmailAddress, 89),
            CreateEmail(users[0], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 65),
            CreateEmail(users[1], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 13),
        };
        await SaveEmails(emails);
        var emailStore = new EmailStore(Testing.DynamoContext);

        var recentEmails = (await emailStore.GetRecentEmails(users[1]))
                           .OrderByDescending(email => email.SentAt)
                           .Select(email => (email.PK, email.SK));

        var expectedEmails = emails
                             .Where(email => email.UserId == users[1].Id && email.SentAt >= DateTimeOffset.Now.AddMinutes(-30))
                             .OrderByDescending(email => email.SentAt)
                             .Select(email => (email.PK, email.SK));
        recentEmails.ShouldBe(expectedEmails);
    }

    private static EmailMessage CreateEmail(User user, string recipient, int minutesAgo) =>
        new(user, recipient, "Test", "Hello!", DateTimeOffset.Now.AddMinutes(-minutesAgo));

    private static Task SaveEmails(EmailMessage[] emails)
    {
        var batch = Testing.DynamoContext.CreateBatchWrite<EmailMessage>();
        batch.AddPutItems(emails);
        return batch.ExecuteAsync();
    }

    private static async Task<EmailMessage?> GetLastEmailAsync(string emailAddress)
    {
        var query = Testing.DynamoContext.QueryAsync<EmailMessage>($"email#{emailAddress.ToUpperInvariant()}");
        var emails = await query.GetRemainingAsync();
        return emails.OrderByDescending(email => email.SentAt).FirstOrDefault();
    }
}
