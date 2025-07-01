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
        var user = CreateUser();
        var store = CreateUserStore();

        var result = await store.CreateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        retrievedUser.Username.ShouldBe(user.Username);
        retrievedUser.NormalizedUsername.ShouldBe(user.NormalizedUsername);
        retrievedUser.EmailAddress.ShouldBe(user.EmailAddress);
        retrievedUser.NormalizedEmailAddress.ShouldBe(user.NormalizedEmailAddress);
        retrievedUser.EmailAddressConfirmed.ShouldBe(user.EmailAddressConfirmed);
        retrievedUser.PasswordHash.ShouldBe(user.PasswordHash);
        retrievedUser.SecurityStamp.ShouldBe(user.SecurityStamp);
        retrievedUser.ConcurrencyStamp.ShouldBe(user.ConcurrencyStamp);
    }

    [Test]
    public async Task ShouldFailToAddUserWhenUserAlreadyExists()
    {
        var user = CreateUser();
        var store = CreateUserStore();

        var result1 = await store.CreateAsync(user, CancellationToken.None);
        var result2 = await store.CreateAsync(user, CancellationToken.None);

        result1.Succeeded.ShouldBeTrue();
        result2.Succeeded.ShouldBeFalse();
        result2.Errors.ShouldNotBeNull();
        result2.Errors.ShouldContain(e => e.Description.Contains("already exists"));
    }

    [Test]
    public async Task ShouldFailToAdduserWhenEmailExists()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        user2.NormalizedEmailAddress = user1.NormalizedEmailAddress;
        var store = CreateUserStore();

        var result1 = await store.CreateAsync(user1, CancellationToken.None);
        var result2 = await store.CreateAsync(user2, CancellationToken.None);

        result1.Succeeded.ShouldBeTrue();
        result2.Succeeded.ShouldBeFalse();
        result2.Errors.ShouldNotBeNull();
        result2.Errors.ShouldContain(e => e.Description.Contains("already exists"));
    }

    [Test]
    public async Task ShouldFailToAdduserWhenUsernameExists()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        user2.NormalizedUsername = user1.NormalizedUsername;
        var store = CreateUserStore();

        var result1 = await store.CreateAsync(user1, CancellationToken.None);
        var result2 = await store.CreateAsync(user2, CancellationToken.None);

        result1.Succeeded.ShouldBeTrue();
        result2.Succeeded.ShouldBeFalse();
        result2.Errors.ShouldNotBeNull();
        result2.Errors.ShouldContain(e => e.Description.Contains("already exists"));
    }

    private static UserStore CreateUserStore() => new(Testing.Dynamo.Client, Testing.Dynamo.Context, Testing.BitsAndBobsTable.FullName);

    private static User CreateUser() =>
        new()
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

    private static Task<User> GetUser(string userId) =>
        Testing.Dynamo.Context.LoadAsync<User>(
            User.GetPk(userId),
            User.SortKey,
            CancellationToken.None
        );
}
