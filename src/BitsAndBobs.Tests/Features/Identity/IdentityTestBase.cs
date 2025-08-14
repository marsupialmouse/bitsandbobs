using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Tests.Features.Identity;

public abstract class IdentityTestBase : TestBase
{
    protected Task<User?> GetUser(UserId id) => new UserStore(Testing.DynamoClient, Testing.DynamoContext).FindByIdAsync(
        id.Value,
        TestContext.CurrentContext.CancellationToken
    );
}
