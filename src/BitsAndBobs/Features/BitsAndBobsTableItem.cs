using Amazon.DynamoDBv2.DataModel;

namespace BitsAndBobs.Features;

[DynamoDBTable("BitsAndBobs")]
public abstract class BitsAndBobsTableItem
{
    [DynamoDBHashKey]
    protected abstract string PK { get; set; }

    [DynamoDBRangeKey]
    protected abstract string SK { get; set; }
}
