using Amazon.DynamoDBv2;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BitsAndBobs.Infrastructure.DynamoDb;

public class DynamoDbHealthCheck(IAmazonDynamoDB dynamo) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await dynamo.ListTablesAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("DynamoDB is not healthy.", e);
        }
    }
}
