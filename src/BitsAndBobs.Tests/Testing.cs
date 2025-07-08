using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using NUnit.Framework;

[assembly: SetCulture("en-AU")]
[assembly: Parallelizable(ParallelScope.Fixtures), LevelOfParallelism(4)]

namespace BitsAndBobs.Tests;


[SetUpFixture]
public class Testing
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static DynamoDb? _dynamo;
    private static DynamoDb.Table? _bitsAndBobsTable;

    [OneTimeSetUp]
    public async Task Setup()
    {
        await Semaphore.WaitAsync();

        try
        {
            if (_dynamo != null)
                throw new InvalidOperationException("DynamoDb already exists.");

            if (_bitsAndBobsTable != null)
                throw new InvalidOperationException("The BitsAndBobs table already exists.");

            _dynamo = await DynamoDb.Create(TestContext.CurrentContext.CancellationToken);

            Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL_DYNAMODB", _dynamo.ConnectionString);

            _bitsAndBobsTable = await _dynamo.CreateTableForCloudFormationResource(
                "DynamoDbTable",
                Path.Combine(ProjectSource.ProjectDirectory(), "../Infrastructure", "aws-resources.yaml"),
                "BitsAndBobs"
            );

            var table = await _dynamo.Client.DescribeTableAsync(_bitsAndBobsTable.FullName);

            Console.WriteLine($"Created DynamoDB table {table.Table.TableName} with GSIs:");
            foreach (var gsi in table.Table.GlobalSecondaryIndexes)
            {
                Console.WriteLine($"- {gsi.IndexName} (ProjectionType: {gsi.Projection.ProjectionType})");
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static DynamoDb.Table BitsAndBobsTable => _bitsAndBobsTable!;
    public static DynamoDb Dynamo => _dynamo!;
    public static IAmazonDynamoDB DynamoClient => _dynamo!.Client;
    public static IDynamoDBContext DynamoContext => _dynamo!.Context;

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await Semaphore.WaitAsync();

        try
        {
            if (_bitsAndBobsTable != null)
                await _bitsAndBobsTable.DisposeAsync();

            if (_dynamo != null)
                await _dynamo.DisposeAsync();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static class ProjectSource
    {
        private static string CallerFilePath([CallerFilePath] string? callerFilePath = null) =>
            callerFilePath ?? throw new ArgumentNullException(nameof(callerFilePath));

        public static string ProjectDirectory() => Path.GetDirectoryName(CallerFilePath())!;
    }
}
