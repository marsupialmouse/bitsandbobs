using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace BitsAndBobs.Infrastructure.DynamoDb;

/// <summary>
/// A locking mechanism that utilises DynamoDB records. Locks are not shared between instances of the client.
/// </summary>
public abstract partial class DynamoDbLockClient(IAmazonDynamoDB dynamo, ILogger<DynamoDbLockClient> logger) : IDistributedLockClient
{
    private const string RangeKey = "Lock";

    private readonly string _clientIdentifier = Guid.NewGuid().ToString("n");



    protected abstract string HashKeyName { get; }
    protected abstract string RangeKeyName { get; }
    protected abstract string TableName { get; }

    /// <summary>
    /// Tries to acquire a lock with the given name. If a lock is acquired an object is returned that can be used to
    /// release the lock.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="timeout">The timer after which the lock is automatically released/invalidated.</param>
    /// <returns>A lock object, if the lock is acquired, or null, if a lock was not acquired.</returns>
    /// <remarks>
    /// Repeated calls of this method on a single instance of the client may extend the lock period, and any one of the
    /// returned objects can be used to release the lock. For this reason it's best not to share instances of the client.
    /// </remarks>
    public async Task<IDistributedLock?> TryAcquireLock(string name, TimeSpan timeout)
    {
        try
        {
            var expiresOn = DateTime.UtcNow.Add(timeout);
            await dynamo.PutItemAsync(CreateLockPutRequest(name, expiresOn));
            return new Lock(name, expiresOn, this);
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return null;
        }
        catch (Exception e)
        {
            Log.ErrorAcquiringLock(logger, name, e);
            return null;
        }
    }

    private async Task ReleaseLock(string name)
    {
        try
        {
            await dynamo.DeleteItemAsync(CreateLockDeleteRequest(name));
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            // The lock has already expired and been gobbled up by some other wretched creature
        }
        catch (Exception e)
        {
            Log.ErrorReleasingLock(logger, name, e);
        }
    }

    private static string Id(string name) => $"lock#{name}";

    private PutItemRequest CreateLockPutRequest(string name, DateTime expiresOn)
    {
        var expirationTime = expiresOn.Ticks;
        var now = DateTime.UtcNow.Ticks;

        return new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { HashKeyName, new AttributeValue(Id(name)) },
                { RangeKeyName, new AttributeValue(RangeKey) },
                { "LockClientId", new AttributeValue(_clientIdentifier) },
                { "LockExpiresOn", new AttributeValue { N = expirationTime.ToString() } },
            },
            ConditionExpression =
                "( attribute_not_exists(#HK) AND attribute_not_exists(#RK) ) OR "
                + "( attribute_exists(#HK) AND attribute_exists(#RK) AND ( LockClientId = :clientId OR LockExpiresOn < :now ) )",
            ExpressionAttributeValues =
                new Dictionary<string, AttributeValue>
                {
                    { ":clientId", new AttributeValue(_clientIdentifier) },
                    { ":now", new AttributeValue { N = now.ToString() } },
                },
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#HK", HashKeyName },
                { "#RK", RangeKeyName },
            },
        };
    }

    private DeleteItemRequest CreateLockDeleteRequest(string name) => new()
    {
        TableName = TableName,
        Key = new Dictionary<string, AttributeValue>
        {
            { HashKeyName, new AttributeValue(Id(name)) },
            { RangeKeyName, new AttributeValue(RangeKey) },
        },
        ConditionExpression = "attribute_exists(#HK) AND attribute_exists(#RK) AND LockClientId = :clientId",
        ExpressionAttributeValues =
            new Dictionary<string, AttributeValue> { { ":clientId", new AttributeValue(_clientIdentifier) } },
        ExpressionAttributeNames = new Dictionary<string, string>
        {
            { "#HK", HashKeyName },
            { "#RK", RangeKeyName },
        },
    };

    private sealed class Lock(string name, DateTime expiresOn, DynamoDbLockClient client) : IDistributedLock
    {
        private DateTime _expiresOn = expiresOn;

        public bool IsActive => _expiresOn >= DateTime.UtcNow;

        public async  Task Release()
        {
            if (IsActive)
            {
                await client.ReleaseLock(name);
                _expiresOn = DateTime.MinValue;
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Message = "Error acquiring lock '{lockName}'", Level = LogLevel.Error)]
        public static partial void ErrorAcquiringLock(ILogger logger, string lockName, Exception e);

        [LoggerMessage(EventId = 2, Message = "Error releasing lock '{lockName}'", Level = LogLevel.Error)]
        public static partial void ErrorReleasingLock(ILogger logger, string lockName, Exception e);
    }
}
