using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Identity;
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
        retrievedUser.ShouldNotBeNull();
        UsersShouldMatch(retrievedUser, user);
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

    [Test]
    public async Task ShouldGetUserByIdWhenUserExists()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByIdAsync(user2.Id, CancellationToken.None);

        user.ShouldNotBeNull();
        UsersShouldMatch(user, user2);
    }

    [Test]
    public async Task ShouldReturnNullUserWhenUserDoesNotExist()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByIdAsync(Guid.NewGuid().ToString("n"), CancellationToken.None);

        user.ShouldBeNull();
    }


    [Test]
    public async Task ShouldGetUserByEmailWhenUserExists()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var user3 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
            store.CreateAsync(user3, CancellationToken.None)
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByEmailAsync(user2.NormalizedEmailAddress!, CancellationToken.None);

        user.ShouldNotBeNull();
        UsersShouldMatch(user, user2);
    }

    [Test]
    public async Task ShouldReturnNullUserWhenNormalizedEmailAddressDoesNotExist()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var user3 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
            store.CreateAsync(user3, CancellationToken.None)
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByEmailAsync(user2.NormalizedEmailAddress!.ToLowerInvariant(), CancellationToken.None);

        user.ShouldBeNull();
    }

    [Test]
    public async Task ShouldGetUserByUsernameWhenUserExists()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var user3 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
            store.CreateAsync(user3, CancellationToken.None)
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByNameAsync(user2.NormalizedUsername!, CancellationToken.None);

        user.ShouldNotBeNull();
        UsersShouldMatch(user, user2);
    }

    [Test]
    public async Task ShouldReturnNullUserWhenNormalizedUsernameDoesNotExist()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var user3 = CreateUser();
        var store = CreateUserStore();
        Task<IdentityResult>[] userTasks =
        [
            store.CreateAsync(user1, CancellationToken.None), store.CreateAsync(user2, CancellationToken.None),
            store.CreateAsync(user3, CancellationToken.None)
        ];
        await Task.WhenAll(userTasks);

        var user = await store.FindByNameAsync(user2.NormalizedUsername!.ToLowerInvariant(), CancellationToken.None);

        user.ShouldBeNull();
    }

    [Test]
    public async Task ShouldDeleteUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();

        await store.CreateAsync(user, CancellationToken.None);
        var result = await store.DeleteAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        retrievedUser.ShouldBeNull();
    }

    [Test]
    public async Task ShouldDeleteEmailRecordWhenDeletingUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();

        await store.CreateAsync(user, CancellationToken.None);
        var result = await store.DeleteAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var emailRecord = await Testing.Dynamo.Client.GetItemAsync(
                              new GetItemRequest
                              {
                                  TableName = Testing.BitsAndBobsTable.FullName,
                                  Key = new Dictionary<string, AttributeValue>
                                  {
                                      { "PK", new AttributeValue { S = $"email#{user.NormalizedEmailAddress}" } },
                                      { "SK", new AttributeValue { S = "Reserved" } },
                                  },
                              },
                              CancellationToken.None
                          );
        emailRecord.Item.ShouldBeNull();
    }

    [Test]
    public async Task ShouldDeleteUsernameRecordWhenDeletingUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();

        await store.CreateAsync(user, CancellationToken.None);
        var result = await store.DeleteAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var emailRecord = await Testing.Dynamo.Client.GetItemAsync(
                              new GetItemRequest
                              {
                                  TableName = Testing.BitsAndBobsTable.FullName,
                                  Key = new Dictionary<string, AttributeValue>
                                  {
                                      { "PK", new AttributeValue { S = $"username#{user.NormalizedUsername}" } },
                                      { "SK", new AttributeValue { S = "Reserved" } },
                                  },
                              },
                              CancellationToken.None
                          );
        emailRecord.Item.ShouldBeNull();
    }

    private static void UsersShouldMatch(User user, User user2)
    {
        user.Username.ShouldBe(user2.Username);
        user.NormalizedUsername.ShouldBe(user2.NormalizedUsername);
        user.EmailAddress.ShouldBe(user2.EmailAddress);
        user.NormalizedEmailAddress.ShouldBe(user2.NormalizedEmailAddress);
        user.EmailAddressConfirmed.ShouldBe(user2.EmailAddressConfirmed);
        user.PasswordHash.ShouldBe(user2.PasswordHash);
        user.SecurityStamp.ShouldBe(user2.SecurityStamp);
        user.ConcurrencyStamp.ShouldBe(user2.ConcurrencyStamp);
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
