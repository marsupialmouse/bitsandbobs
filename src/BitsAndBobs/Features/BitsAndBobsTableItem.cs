using Amazon.DynamoDBv2.DataModel;

namespace BitsAndBobs.Features;

[DynamoDBTable("BitsAndBobs")]
public abstract class BitsAndBobsTableItem
{
    /// <summary>
    /// The primary key for the item in the BitsAndBobs table.
    /// </summary>
    [DynamoDBHashKey]
    public abstract string PK { get; protected set; }

    /// <summary>
    /// The sort key for the item in the BitsAndBobs table.
    /// </summary>
    [DynamoDBRangeKey]
    public abstract string SK { get; protected set; }
}
