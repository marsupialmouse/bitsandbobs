using BitsAndBobs.Infrastructure.DynamoDb;

namespace BitsAndBobs.Infrastructure;

public interface IDistributedLockClient
{
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
    Task<IDistributedLock?> TryAcquireLock(string name, TimeSpan timeout);
}

public interface IDistributedLock
{
    bool IsActive { get; }

    Task Release();
}
