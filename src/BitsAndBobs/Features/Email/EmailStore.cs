using System.Net;
using System.Text.Encodings.Web;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Email;

public class EmailStore : IEmailStore, IEmailSender<User>
{
    private readonly IDynamoDBContext _context;

    public EmailStore(IDynamoDBContext context)
    {
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

        return _context.SaveAsync(message);
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

        return _context.SaveAsync(message);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var message = new EmailMessage(
            user,
            email,
            "Password Reset Link",
            $"Please reset your password by [clicking here](/resetpassword?email={WebUtility.UrlEncode(email)}&code={resetCode})."
        );

        return _context.SaveAsync(message);
    }

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
}
