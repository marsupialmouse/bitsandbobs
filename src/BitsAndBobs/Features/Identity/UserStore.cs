using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public class UserStore : IUserStore<User>, IUserEmailStore<User>, IUserPasswordStore<User>
{
    private readonly IAmazonDynamoDB _db;
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

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
            const string uniqueItemCondition = "attribute_not_exists(PK) AND attribute_not_exists(SK)";

            var items = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = _context.ToDocument(user).ToAttributeMap(),
                        ConditionExpression = uniqueItemCondition
                    }
                },
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = GetReservedEmailItem(user),
                        ConditionExpression = uniqueItemCondition
                    }
                },
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = GetReservedUsernameItem(user),
                        ConditionExpression = uniqueItemCondition
                    }
                }
            };

            await _db.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = items },
                cancellationToken
            );

            return IdentityResult.Success;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUser", Description = "User already exists" });
        }
    }

    private static Dictionary<string, AttributeValue> GetReservedEmailItem(User user) =>
        GetReservedUserItem(user, $"email#{user.NormalizedEmailAddress!}");

    private static Dictionary<string, AttributeValue> GetReservedUsernameItem(User user) =>
        GetReservedUserItem(user, $"username#{user.NormalizedUsername!}");

    private static Dictionary<string, AttributeValue> GetReservedUserItem(User user, string pk) =>
        new()
        {
            { "PK", new AttributeValue { S = pk } },
            { "SK", new AttributeValue { S = "Reserved" } },
            { "UserId", new AttributeValue { S = user.PK } }
        };

    public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) => _context.LoadAsync<User>(
        User.GetPk(userId), User.SortKey,
        cancellationToken
    )!;

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var query = _context.QueryAsync<User>(
            normalizedUserName,
            new QueryConfig { IndexName = "UsersByNormalizedUsername" }
        );

        return (await query.GetRemainingAsync(cancellationToken)).SingleOrDefault();
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var query = _context.QueryAsync<User>(
            normalizedEmail,
            new QueryConfig { IndexName = "UsersByNormalizedEmailAddress" }
        );

        return (await query.GetRemainingAsync(cancellationToken)).SingleOrDefault();
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Username)!;
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUsername);
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.EmailAddress)!;
    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmailAddress);
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.EmailAddressConfirmed);
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

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

    public void Dispose()
    {
    }
}
