using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Features;

public static class BitsAndBobsTable
{
    /// <summary>
    /// The name of the table (the base name - to match the table attribute)
    /// </summary>
    public const string Name = "BitsAndBobs";

    /// <summary>
    /// The full name of the table (including any prefix).
    /// </summary>
    public static string FullName { get; private set; } = Name;

    /// <summary>
    /// Creates the table definition for the BitsAndBobs table in DynamoDB.
    /// </summary>
    public static Table CreateTableDefinition(IAmazonDynamoDB client, string? tableNamePrefix = null)
    {
        if (!string.IsNullOrEmpty(tableNamePrefix))
            FullName = $"{tableNamePrefix}{Name}";

        return new TableBuilder(client, FullName)
               .AddHashKey("PK", DynamoDBEntryType.String)
               .AddRangeKey("SK", DynamoDBEntryType.String)
               .AddGlobalSecondaryIndex(
                   "UsersByNormalizedEmailAddress",
                   "NormalizedEmailAddress",
                   DynamoDBEntryType.String
               )
               .AddGlobalSecondaryIndex("UsersByNormalizedUsername", "NormalizedUsername", DynamoDBEntryType.String)
               .AddGlobalSecondaryIndex(
                   "AuctionsByStatus",
                   "AuctionStatus",
                   DynamoDBEntryType.Numeric,
                   "EndDateUtcTimeStamp",
                   DynamoDBEntryType.Numeric
               )
               .AddGlobalSecondaryIndex(
                   "AuctionsBySeller",
                   "SellerId",
                   DynamoDBEntryType.String
               )
               .AddGlobalSecondaryIndex(
                   "AuctionsByCurrentBidder",
                   "CurrentBidderId",
                   DynamoDBEntryType.String,
                   "AuctionStatus",
                   DynamoDBEntryType.Numeric
               )
               .AddGlobalSecondaryIndex(
                   "UserAuctionBidsByDate",
                   "PK",
                   DynamoDBEntryType.String,
                   "LastUserBidUtcTimestamp",
                   DynamoDBEntryType.Numeric
               )
               .AddGlobalSecondaryIndex(
                   "EmailsByRecipientUser",
                   "RecipientUserId",
                   DynamoDBEntryType.String,
                   "SentAtUtcTimeStamp",
                   DynamoDBEntryType.Numeric
               )
               .AddGlobalSecondaryIndex(
                   "EmailsByRecipientEmail",
                   "NormalizedRecipientEmail",
                   DynamoDBEntryType.String,
                   "SentAtUtcTimeStamp",
                   DynamoDBEntryType.Numeric
               )
               .Build();
    }

    /// <summary>
    /// Saves an <see cref="Item"/> entity with the correct type so that the polymorphic attribute will work.
    /// </summary>
    public static Task SaveItem<T>(this IDynamoDBContext context, T entity) where T : Item =>
        context.SaveAsync<Item>(entity);

    /// <summary>
    /// Creates a <see cref="Put"/> object to insert a new entity.
    /// </summary>
    public static Put CreateInsertPut<T>(this IDynamoDBContext context, T entity) where T : Item => new()
    {
        TableName = FullName,
        Item = context.ToDocument<Item>(entity).ToAttributeMap(),
        ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)",
    };

    /// <summary>
    /// Creates a <see cref="Put"/> object to update an existing entity with a concurrency check.
    /// </summary>
    public static Put CreateUpdatePut<T>(this IDynamoDBContext context, T entity) where T : VersionedEntity => new()
    {
        TableName = FullName,
        Item = context.ToDocument<Item>(entity).ToAttributeMap(),
        ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK) AND Version = :currentVersion",
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":currentVersion", new AttributeValue(entity.InitialVersion) },
        },
    };

    /// <summary>
    /// Creates a <see cref="Put"/> object to insert or update a new entity.
    /// </summary>
    public static Put CreateUpsertPut<T>(this IDynamoDBContext context, T entity) where T : Item => new()
    {
        TableName = FullName,
        Item = context.ToDocument<Item>(entity).ToAttributeMap(),
    };

    [DynamoDBPolymorphicType("A", typeof(Auction))]
    [DynamoDBPolymorphicType("B", typeof(Bid))]
    [DynamoDBPolymorphicType("E", typeof(EmailMessage))]
    [DynamoDBPolymorphicType("I", typeof(AuctionImage))]
    [DynamoDBPolymorphicType("L", typeof(UserAuctionBid))]
    [DynamoDBPolymorphicType("U", typeof(User))]
    [DynamoDBTable(Name)]
    public class Item
    {
        /// <summary>
        /// The HashKey attribute. Any class that inherits from this class should implement this property.
        /// </summary>
        /// <remarks>
        /// This should be abstract, but the DynamoDB SDK doesn't like abstract classes.
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        protected virtual string PK { get; set; } = "";

        /// <summary>
        /// The RangeKey attribute. Any class that inherits from this class should implement this property.
        /// </summary>
        /// <remarks>
        /// This should be abstract, but the DynamoDB SDK doesn't like abstract classes.
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        protected virtual string SK { get; set; } = "";
    }

    public abstract class VersionedEntity : Item
    {
        /// <summary>
        /// A random value that must change whenever a user is persisted to the store
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
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
}
