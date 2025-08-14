using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Tests.Features.Identity;

public abstract class IdentityTestBase : TestBase
{
    protected async Task<User> CreateUser(string? firstName = null, string? lastName = null, string? displayName = null, string? emailAddress = null)
    {
        emailAddress ??= $"test-{Guid.NewGuid()}@example.com";

        var user = new User
        {
            EmailAddress = emailAddress,
            NormalizedEmailAddress = emailAddress.ToUpperInvariant(),
            Username = emailAddress,
            NormalizedUsername = emailAddress.ToUpperInvariant(),
            EmailAddressConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName!,
        };
        await new UserStore(Testing.DynamoClient, Testing.DynamoContext).
              CreateAsync(user, TestContext.CurrentContext.CancellationToken).ConfigureAwait(false);

        return user;
    }

    protected Task<User?> GetUser(UserId id) => new UserStore(Testing.DynamoClient, Testing.DynamoContext).FindByIdAsync(
        id.Value,
        TestContext.CurrentContext.CancellationToken
    );
}
