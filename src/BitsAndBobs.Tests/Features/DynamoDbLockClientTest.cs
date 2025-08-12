using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace BitsAndBobs.Tests.Features;

[TestFixture]
public class DynamoDbLockClientTest
{
    [Test]
    public async Task ShouldAcquireLock()
    {
        var client = CreateClient();

        var dbLock = await client.TryAcquireLock(Guid.NewGuid().ToString(), TimeSpan.FromMinutes(1));

        dbLock.ShouldNotBeNull();
        dbLock.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldFailToAcquireLockWithDifferentClient()
    {
        var client1 = CreateClient();
        var client2 = CreateClient();
        var lockName = Guid.NewGuid().ToString();

        var lock1 = await client1.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));
        var lock2 = await client2.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));

        lock1.ShouldNotBeNull();
        lock1.IsActive.ShouldBeTrue();
        lock2.ShouldBeNull();
    }

    [Test]
    public async Task ShouldExtendLockWithSameClient()
    {
        var client = CreateClient();
        var lockName = Guid.NewGuid().ToString();

        await client.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));
        var acquiredLock = await client.TryAcquireLock(lockName, TimeSpan.FromMinutes(10));
        var dbLock = await GetLock(lockName);

        acquiredLock.ShouldNotBeNull();
        dbLock.ShouldNotBeNull();
        dbLock.ExpiryUtc.ShouldBe(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task ShouldAcquireLockWithDifferentClientWhenFirstLockExpired()
    {
        var client1 = CreateClient();
        var client2 = CreateClient();
        var lockName = Guid.NewGuid().ToString();

        var lock1 = await client1.TryAcquireLock(lockName, TimeSpan.FromMilliseconds(100));
        await Task.Delay(TimeSpan.FromMilliseconds(150));
        var lock2 = await client2.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));

        lock1.ShouldNotBeNull();
        lock1.IsActive.ShouldBeFalse();
        lock2.ShouldNotBeNull();
        lock2.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldAcquireLockWithDifferentClientWhenFirstLockReleased()
    {
        var client1 = CreateClient();
        var client2 = CreateClient();
        var lockName = Guid.NewGuid().ToString();

        var lock1 = await client1.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));
        await lock1!.Release();
        var lock2 = await client2.TryAcquireLock(lockName, TimeSpan.FromMinutes(1));

        lock1.IsActive.ShouldBeFalse();
        lock2.ShouldNotBeNull();
        lock2.IsActive.ShouldBeTrue();
    }

    private static Task<Lock?> GetLock(string name) =>
        Testing.DynamoContext.LoadAsync<Lock>($"lock#{name}", "Lock")!;

    private static DynamoDbLockClient CreateClient() => new BitsAndBobsTable.LockClient(
        Testing.DynamoClient,
        Substitute.For<ILogger<DynamoDbLockClient>>()
    );

    [DynamoDBTable(BitsAndBobsTable.Name)]
    public class Lock
    {
        // ReSharper disable UnusedMember.Global
        // ReSharper disable InconsistentNaming
        // ReSharper disable PropertyCanBeMadeInitOnly.Global
        public string PK { get; set; } = "";
        public string SK { get; set; } = "";
        public string LockClientId { get; set; } = "";
        public long LockExpiresOn { get; set; }
        // ReSharper restore PropertyCanBeMadeInitOnly.Global
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global

        public DateTime ExpiryUtc => new(LockExpiresOn, DateTimeKind.Utc);
    }
}
