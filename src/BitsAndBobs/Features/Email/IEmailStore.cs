using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Features.Email;

public interface IEmailStore
{
    /// <summary>
    /// Gets emails recently sent to a specific email address.
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetRecentEmails(string emailAddress);

    /// <summary>
    /// Gets emails recently sent to a specific user.
    /// </summary>
    Task<IEnumerable<EmailMessage>> GetRecentEmails(User user);
}
