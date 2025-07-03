using System.Collections.Concurrent;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public class UserStore : IUserEmailStore<User>, IUserPasswordStore<User>, IUserSecurityStampStore<User>, IUserLockoutStore<User>
{
    private const string UniqueItemCondition = "attribute_not_exists(PK) AND attribute_not_exists(SK)";

    private readonly IAmazonDynamoDB _db;
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    private readonly ConcurrentDictionary<string, UserMemo> _uniqueAttributesCache = new();

    public UserStore(IAmazonDynamoDB db, IDynamoDBContext context, string tableName)
    {
        _db = db;
        _context = context;
        _tableName = tableName;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            UpdateVersion(user);

            var items = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = _context.ToDocument(user).ToAttributeMap(),
                        ConditionExpression = UniqueItemCondition
                    }
                },
                new() { Put = GetReservedEmailPut(user) },
                new() { Put = GetReservedUsernamePut(user) },
            };

            await _db.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = items },
                cancellationToken
            );

            CacheUniqueAttributes(user);

            return IdentityResult.Success;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUser", Description = "User already exists" });
        }
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (!_uniqueAttributesCache.TryGetValue(user.Id, out var userMemo))
            throw new InvalidOperationException("User not found in cache.");

        try
        {
            var currentVersion = user.Version;
            UpdateVersion(user);

            var items = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = _context.ToDocument(user).ToAttributeMap(),
                        ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK) AND Version = :currentVersion",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":currentVersion", new AttributeValue(currentVersion) },
                        },
                    },
                },
            };

            if (userMemo.NormalizedEmailAddress != user.NormalizedEmailAddress)
            {
                items.Add(new TransactWriteItem { Delete = GetReservedEmailDelete(userMemo) });
                items.Add(new TransactWriteItem { Put = GetReservedEmailPut(user) });
            }

            if (userMemo.NormalizedUsername != user.NormalizedUsername)
            {
                items.Add(new TransactWriteItem { Delete = GetReservedUsernameDelete(userMemo) });
                items.Add(new TransactWriteItem { Put = GetReservedUsernamePut(user) });
            }

            await _db.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = items },
                cancellationToken
            );

            return IdentityResult.Success;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "ConcurrencyFailure",
                    Description = "Optimistic concurrency failure, object has been modified.",
                }
            );
        }
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var items = new List<TransactWriteItem>
            {
                new()
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            { nameof(user.PK), new AttributeValue(user.PK) },
                            { nameof(user.SK), new AttributeValue(user.SK) },
                        },
                    },
                },
                new() { Delete = GetReservedEmailDelete(user) },
                new() { Delete = GetReservedUsernameDelete(user) },
            };

            await _db.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = items },
                cancellationToken
            );

            _uniqueAttributesCache.TryRemove(user.Id, out _);

            return IdentityResult.Success;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "ConcurrencyFailure",
                    Description = "Optimistic concurrency failure, object has been modified.",
                }
            );
        }
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _context.LoadAsync<User>(User.GetPk(userId), User.SortKey, cancellationToken);
        CacheUniqueAttributes(user);
        return user;
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var query = _context.QueryAsync<User>(
            normalizedUserName,
            new QueryConfig { IndexName = "UsersByNormalizedUsername" }
        );

        var user = (await query.GetRemainingAsync(cancellationToken)).SingleOrDefault();
        CacheUniqueAttributes(user);
        return user;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var query = _context.QueryAsync<User>(
            normalizedEmail,
            new QueryConfig { IndexName = "UsersByNormalizedEmailAddress" }
        );

        var user = (await query.GetRemainingAsync(cancellationToken)).SingleOrDefault();
        CacheUniqueAttributes(user);
        return user;
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Username)!;

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUsername);

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailAddress)!;

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmailAddress);

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailAddressConfirmed);

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEndDate);

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.FailedAccessAttempts);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.IsLockedOut);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? throw new ArgumentNullException(nameof(userName), "User name cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUsername = normalizedName ?? throw new ArgumentNullException(nameof(normalizedName), "NormalizedUserName cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.EmailAddress = email ?? throw new ArgumentNullException(nameof(email), "Email cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailAddressConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmailAddress = normalizedEmail ?? throw new ArgumentNullException(nameof(normalizedEmail), "Normalized email cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEndDate = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.FailedAccessAttempts++;
        return Task.FromResult(user.FailedAccessAttempts);
    }

    public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.FailedAccessAttempts = 0;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.IsLockedOut = enabled;
        return Task.CompletedTask;
    }

        private Put GetReservedEmailPut(User user) => GetReservedItemPut(user, GetReservedEmailKeyAttributes);

    private Put GetReservedUsernamePut(User user) => GetReservedItemPut(user, GetReservedUsernameKeyAttributes);

    private Put GetReservedItemPut(User user, Func<User, Dictionary<string, AttributeValue>> getKeyAttributes)
    {
        var item = getKeyAttributes(user);

        item["UserId"] = new AttributeValue(user.PK);

        return new Put
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = UniqueItemCondition,
        };
    }

    private Delete GetReservedEmailDelete(User user) =>
        GetReservedItemDelete(user.PK, GetReservedEmailKeyAttributes(user));

    private Delete GetReservedEmailDelete(UserMemo user) =>
        GetReservedItemDelete(user.PK, GetReservedEmailKeyAttributes(user));

    private Delete GetReservedUsernameDelete(User user) =>
        GetReservedItemDelete(user.PK, GetReservedUsernameKeyAttributes(user));

    private Delete GetReservedUsernameDelete(UserMemo user) =>
        GetReservedItemDelete(user.PK, GetReservedUsernameKeyAttributes(user));

    private Delete GetReservedItemDelete(string userPk, Dictionary<string, AttributeValue> keyAttributes) =>
        new()
        {
            TableName = _tableName,
            Key = keyAttributes,
            ConditionExpression = "UserId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":userId", new AttributeValue(userPk) },
            },
        };

    private static Dictionary<string, AttributeValue> GetReservedEmailKeyAttributes(User user) =>
        GetReservedEmailKeyAttributes(user.NormalizedEmailAddress);

    private static Dictionary<string, AttributeValue> GetReservedEmailKeyAttributes(UserMemo user) =>
        GetReservedEmailKeyAttributes(user.NormalizedEmailAddress);

    private static Dictionary<string, AttributeValue> GetReservedEmailKeyAttributes(string? normalizedEmailAddress) =>
        GetReservedKeyAttributes($"email#{normalizedEmailAddress}");

    private static Dictionary<string, AttributeValue> GetReservedUsernameKeyAttributes(User user) =>
        GetReservedUsernameKeyAttributes(user.NormalizedUsername);

    private static Dictionary<string, AttributeValue> GetReservedUsernameKeyAttributes(UserMemo user) =>
        GetReservedUsernameKeyAttributes(user.NormalizedUsername);

    private static Dictionary<string, AttributeValue> GetReservedUsernameKeyAttributes(string? normalizedUsername) =>
        GetReservedKeyAttributes($"username#{normalizedUsername}");

    private static Dictionary<string, AttributeValue> GetReservedKeyAttributes(string pk) =>
        new()
        {
            { "PK", new AttributeValue(pk) },
            { "SK", new AttributeValue("Reserved") },
        };

    private static void UpdateVersion(User user) => user.Version = Guid.NewGuid().ToString("n");

    private void CacheUniqueAttributes(User? user)
    {
        if (user is null)
            return;

        _uniqueAttributesCache.AddOrUpdate(
            user.Id,
            static (_, u) => new UserMemo(u),
            static (_, _, u) => new UserMemo(u),
            user
        );
    }

    public void Dispose()
    {
    }

    private record UserMemo(string PK, string? NormalizedEmailAddress, string? NormalizedUsername)
    {
        public UserMemo(User user) : this(user.PK, user.NormalizedEmailAddress, user.NormalizedUsername)
        {
        }
    }
}
