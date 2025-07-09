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
            "Password Reset Code",
            $"Please reset your password using the following code: {resetCode}"
        );

        return _context.SaveAsync(message);
    }

    public async Task<IEnumerable<EmailMessage>> GetRecentEmails(string emailAddress)
    {
        var query = _context.QueryAsync<EmailMessage>(
            $"email#{emailAddress.ToUpperInvariant()}",
            QueryOperator.GreaterThan,
            [DateTime.UtcNow.AddDays(-1).ToString("O")]
        );

        return await query.GetRemainingAsync();
    }

    public async Task<IEnumerable<EmailMessage>> GetRecentEmails(User user)
    {
        var query = _context.QueryAsync<EmailMessage>(
            user.PK,
            QueryOperator.GreaterThan,
            [DateTime.UtcNow.AddDays(-1).ToString("O")],
            new QueryConfig { IndexName = "EmailsByUserId" }
        );

        return await query.GetRemainingAsync();
    }
}
