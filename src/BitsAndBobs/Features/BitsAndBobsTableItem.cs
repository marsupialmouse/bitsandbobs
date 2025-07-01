using Amazon.DynamoDBv2.DataModel;

namespace BitsAndBobs.Features;

[DynamoDBTable("BitsAndBobs")]
public abstract class BitsAndBobsTableItem
{
    [DynamoDBHashKey]
    public abstract string PK { get; protected set; }

    [DynamoDBRangeKey]
    public abstract string SK { get; protected set; }
}
