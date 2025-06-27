using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

public class UserStoreTests
{
    [Test]
    public async Task ShouldSaveUser()
    {
        var user = new User
        {
            Username = Guid.NewGuid().ToString(),
            NormalizedUsername = Guid.NewGuid().ToString().ToUpperInvariant(),
            EmailAddress = $"{Guid.NewGuid()}@example.com",
            NormalizedEmailAddress = $"{Guid.NewGuid()}@example.com".ToUpperInvariant(),
            EmailAddressConfirmed = true,
            PasswordHash = "hashed-password",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };
        var store = new UserStore(Testing.Dynamo.Client, Testing.Dynamo.Context);

        await store.CreateAsync(user, CancellationToken.None);

        var retrievedUser = await Testing.Dynamo.Context.LoadAsync<User>(
                                User.GetPk(user.Id),
                                User.SortKey,
                                new LoadConfig { OverrideTableName = Testing.BitsAndBobsTable.Name },
                                CancellationToken.None
                            );

        retrievedUser.Username.ShouldBe(user.Username);
        retrievedUser.NormalizedUsername.ShouldBe(user.NormalizedUsername);
        retrievedUser.EmailAddress.ShouldBe(user.EmailAddress);
        retrievedUser.NormalizedEmailAddress.ShouldBe(user.NormalizedEmailAddress);
        retrievedUser.EmailAddressConfirmed.ShouldBe(user.EmailAddressConfirmed);
        retrievedUser.PasswordHash.ShouldBe(user.PasswordHash);
        retrievedUser.SecurityStamp.ShouldBe(user.SecurityStamp);
        retrievedUser.ConcurrencyStamp.ShouldBe(user.ConcurrencyStamp);
    }
}
