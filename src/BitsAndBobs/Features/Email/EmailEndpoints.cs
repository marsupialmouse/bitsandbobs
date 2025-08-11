using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Email;

public static class EmailEndpoints
{
    public sealed record EmailResponse(
        [property: Required] string Recipient,
        [property: Required] string Type,
        [property: Required] string Body,
        [property: Required] DateTimeOffset SentAt
    );

    /// <summary>
    /// Maps endpoints for retrieving emails (since we don't want to mess around with actually sending emails).
    /// </summary>
    public static void MapEmailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/emails/recent/",
            async Task<Results<Ok<IEnumerable<EmailResponse>>, NotFound>> (ClaimsPrincipal claimsPrincipal, UserManager<User> userManager, IEmailStore emailStore) =>
            {
                if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
                {
                    return TypedResults.NotFound();
                }
                var emails = await emailStore.GetRecentEmails(user);

                return TypedResults.Ok(emails.ToEmailResponses());
            }
        ).RequireAuthorization();

        endpoints.MapGet(
            "/emails/recent/{emailAddress}",
            async (string emailAddress, IEmailStore emailStore) =>
            {
                var emails = await emailStore.GetRecentEmails(emailAddress);
                return emails.ToEmailResponses();
            }
        );
    }

    private static IEnumerable<EmailResponse> ToEmailResponses(this IEnumerable<EmailMessage> emails) =>
        emails
            .OrderByDescending(x => x.SentAt)
            .Select(email => new EmailResponse(email.RecipientEmail, email.Type, email.Body, email.SentAt));
}
