using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features;

namespace BitsAndBobs.Infrastructure.DynamoDb;

public static class DynamoDbContextExtensions
{
    /// <summary>
    /// Creates a <see cref="Put"/> object to u[date an existing entity with a concurrency check.
    /// </summary>
    public static Put CreateUpdatePut<T>(this IDynamoDBContext context, T entity) where T : VersionedEntity => new()
    {
        TableName = BitsAndBobsTable.FullName,
        Item = context.ToDocument(entity).ToAttributeMap(),
        ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK) AND Version = :currentVersion",
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":currentVersion", new AttributeValue(entity.InitialVersion) },
        },
    };
}
