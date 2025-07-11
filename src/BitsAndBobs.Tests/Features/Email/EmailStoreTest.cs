using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Email;

[TestFixture]
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
        email.RecipientUserId.ShouldBe(user.Id);
        email.RecipientEmail.ShouldBe(emailAddress);
        email.Type.ShouldBe("Email Confirmation");
        email.SentAt.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldReplaceEmailConfirmationLinkWithPathAndQuery()
    {
        const string emailAddress = "email-confirm-link@fufme.com";
        var user = new User();
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendConfirmationLinkAsync(user, emailAddress, "https://example.com/confirm/path?token=123&amp;a=b");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.Body.ShouldContain("[clicking here](/confirmemail?token=123&a=b)");
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
        email.RecipientUserId.ShouldBe(user.Id);
        email.RecipientEmail.ShouldBe(emailAddress);
        email.Type.ShouldBe("Password Reset Link");
        email.SentAt.ShouldBe(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldReplacePasswordResetLinkWithPathAndQuery()
    {
        const string emailAddress = "password-resetlink-link@fufme.com";
        var user = new User();
        var emailStore = new EmailStore(Testing.DynamoContext);

        await emailStore.SendPasswordResetLinkAsync(user, emailAddress, "https://example.com/reset/path?token=321&amp;v=a");

        var email = await GetLastEmailAsync(emailAddress);
        email.ShouldNotBeNull();
        email.Body.ShouldContain("[clicking here](/resetpassword?token=321&v=a)");
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
        email.RecipientUserId.ShouldBe(user.Id);
        email.RecipientEmail.ShouldBe(emailAddress);
        email.Type.ShouldBe("Password Reset Link");
        email.Body.ShouldContain($"[clicking here](/resetpassword?email=email-resetcode%40fufme.com&code=parsnip)");
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
            CreateEmail(users[0], users[0].EmailAddress, 1200),
            CreateEmail(users[0], users[0].EmailAddress, 1600),
            CreateEmail(users[0], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 1500),
            CreateEmail(users[1], users[1].EmailAddress, 15),
            CreateEmail(users[1], users[0].EmailAddress, 200),
        };
        await SaveEmails(emails);
        var emailStore = new EmailStore(Testing.DynamoContext);

        var recentEmails = (await emailStore.GetRecentEmails(users[0].EmailAddress))
            .OrderByDescending(email => email.SentAt)
            .Select(email => (email.PK, email.SK));

        var expectedEmails = emails
            .Where(email => email.RecipientEmail == users[0].EmailAddress && email.SentAt >= DateTimeOffset.Now.AddDays(-1))
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
            CreateEmail(users[0], users[0].EmailAddress, 101),
            CreateEmail(users[0], users[0].EmailAddress, 1600),
            CreateEmail(users[0], users[1].EmailAddress, 1),
            CreateEmail(users[1], users[0].EmailAddress, 1500),
            CreateEmail(users[1], users[1].EmailAddress, 2),
            CreateEmail(users[1], users[0].EmailAddress, 130),
        };
        await SaveEmails(emails);
        var emailStore = new EmailStore(Testing.DynamoContext);

        var recentEmails = (await emailStore.GetRecentEmails(users[1]))
                           .OrderByDescending(email => email.SentAt)
                           .Select(email => (email.PK, email.SK));

        var expectedEmails = emails
                             .Where(email => email.RecipientUserId == users[1].Id && email.SentAt >= DateTimeOffset.Now.AddDays(-1))
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
