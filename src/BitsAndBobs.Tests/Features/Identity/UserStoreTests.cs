using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

[TestFixture]
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

        var user = await store.FindByIdAsync(user2.Id.Value, CancellationToken.None);

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
        (await GetEmailRecord(user.NormalizedEmailAddress!)).ShouldBeNull();
    }

    [Test]
    public async Task ShouldDeleteUsernameRecordWhenDeletingUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);

        var result = await store.DeleteAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        (await GetUsernameRecord(user.NormalizedUsername!)).ShouldBeNull();
    }

    [Test]
    public async Task ShouldUpdateUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        user.EmailAddressConfirmed = !user.EmailAddressConfirmed;
        user.PasswordHash = "new-hashed-password";
        user.SecurityStamp = Guid.NewGuid().ToString();

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        UsersShouldMatch(retrievedUser, user);
    }

    [Test]
    public async Task ShouldNotUpdateUserWhenConcurrencyCheckFails()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        var altStore = CreateUserStore();
        var altUser = (await altStore.FindByIdAsync(user.Id.Value, CancellationToken.None))!;
        altUser.PasswordHash = "alt-hashed-password";
        await altStore.UpdateAsync(altUser, CancellationToken.None);
        user.PasswordHash = "new-hashed-password";

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.Any(e => e.Code == "ConcurrencyFailure").ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        UsersShouldMatch(retrievedUser, altUser);
    }

    [Test]
    public async Task ShouldDoNothingIfUpdatingNonExistentUser()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        await Testing.DynamoContext.DeleteAsync(user, CancellationToken.None);

        var result = await store.UpdateAsync(user, CancellationToken.None);

        var retrievedUser = await GetUser(user.Id);
        retrievedUser.ShouldBeNull();
        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldReplaceEmailRecordWhenEmailAddressChanges()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        var originalEmail = user.NormalizedEmailAddress!;
        user.EmailAddress = $"{Guid.NewGuid()}@example.com";
        user.NormalizedEmailAddress = user.EmailAddress.ToUpperInvariant();

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        UsersShouldMatch(retrievedUser, user);
        (await GetEmailRecord(originalEmail)).ShouldBeNull();
        (await GetEmailRecord(user.NormalizedEmailAddress)).ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldReplaceUsernameRecordWhenUsernameChanges()
    {
        var user = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        var originalUsername = user.NormalizedUsername!;
        user.Username = Guid.NewGuid().ToString();
        user.NormalizedUsername = user.Username.ToUpperInvariant();

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        var retrievedUser = await GetUser(user.Id);
        UsersShouldMatch(retrievedUser, user);
        (await GetUsernameRecord(originalUsername)).ShouldBeNull();
        (await GetUsernameRecord(user.NormalizedUsername)).ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldFailToUpdateUserIfChangedEmailExists()
    {
        var user = CreateUser();
        var user2 = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        await store.CreateAsync(user2, CancellationToken.None);
        user.EmailAddress = user2.EmailAddress;
        user.NormalizedEmailAddress = user2.NormalizedEmailAddress;

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldFailToUpdateUserIfChangedUsernameExists()
    {
        var user = CreateUser();
        var user2 = CreateUser();
        var store = CreateUserStore();
        await store.CreateAsync(user, CancellationToken.None);
        await store.CreateAsync(user2, CancellationToken.None);
        user.Username = user2.Username;
        user.NormalizedUsername = user2.NormalizedUsername;

        var result = await store.UpdateAsync(user, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
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
        user.Version.ShouldBe(user2.Version);
        user.IsLockedOut.ShouldBe(user2.IsLockedOut);
        user.LockoutEndDate.ShouldBe(user2.LockoutEndDate);
        user.FailedAccessAttempts.ShouldBe(user2.FailedAccessAttempts);
    }

    private static UserStore CreateUserStore() => new(Testing.DynamoClient, Testing.DynamoContext);

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
            IsLockedOut = true,
            LockoutEndDate = DateTimeOffset.Now.AddMinutes(4622),
            FailedAccessAttempts = 4,
        };

    private static Task<User> GetUser(UserId userId) =>
        Testing.DynamoContext.LoadAsync<User>(userId, User.SortKey, CancellationToken.None);

    private static Task<Dictionary<string, AttributeValue>?> GetEmailRecord(string normalizedEmail) =>
        GetReservedItemRecord($"emailaddress#{normalizedEmail}");

    private static Task<Dictionary<string, AttributeValue>?> GetUsernameRecord(string normalizedUsername) =>
        GetReservedItemRecord($"username#{normalizedUsername}");

    private static async Task<Dictionary<string, AttributeValue>?> GetReservedItemRecord(string pk)
    {
        var result = await Testing.DynamoClient.GetItemAsync(
                         new GetItemRequest
                         {
                             TableName = Testing.BitsAndBobsTable.FullName,
                             Key = new Dictionary<string, AttributeValue>
                             {
                                 { "PK", new AttributeValue { S = pk } },
                                 { "SK", new AttributeValue { S = "Reserved" } },
                             },
                         },
                         CancellationToken.None
                     );

         return result?.Item;
    }
}
