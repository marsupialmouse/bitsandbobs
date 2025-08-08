using Amazon.DynamoDBv2.DataModel;

namespace BitsAndBobs.Infrastructure.DynamoDb;

public abstract class VersionedEntity
{
    /// <summary>
    /// A random value that must change whenever a user is persisted to the store
    /// </summary>
    public string Version { get; protected set; } = "";

    /// <summary>
    /// Gets the version string before the last update (for concurrency control)
    /// </summary>
    [DynamoDBIgnore]
    public string InitialVersion { get; private set; } = "";

    protected void UpdateVersion()
    {
        InitialVersion = Version;
        Version = Guid.NewGuid().ToString("n");
    }
}
