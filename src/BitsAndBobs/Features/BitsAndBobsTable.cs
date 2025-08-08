using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

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
                   "EmailsByUserId",
                   "RecipientUserId",
                   DynamoDBEntryType.String,
                   "SK",
                   DynamoDBEntryType.String
               )
                .AddGlobalSecondaryIndex(
                     "AuctionsByStatus",
                     "AuctionStatus",
                     DynamoDBEntryType.Numeric,
                     "EndDateUtcTimeStamp",
                     DynamoDBEntryType.Numeric
                )
               .Build();
    }
}
