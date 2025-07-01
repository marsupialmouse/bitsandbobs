using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Testcontainers.DynamoDb;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace BitsAndBobs.Tests;

public sealed class DynamoDb : IAsyncDisposable
{
    private readonly DynamoDbContainer _container;

    private DynamoDb(DynamoDbContainer container)
    {
        _container = container;
        ConnectionString = container.GetConnectionString();
        TablePrefix = $"{Guid.NewGuid():n}-";
        Client = new AmazonDynamoDBClient(
            new AmazonDynamoDBConfig
            {
                ServiceURL = ConnectionString,
                UseHttp = true,
                DefaultAWSCredentials = new BasicAWSCredentials("dummy", "dummy"),
            }
        );
        Context = new DynamoDBContextBuilder().ConfigureContext(config => { config.TableNamePrefix = TablePrefix; })
                                              .WithDynamoDBClient(() => Client)
                                              .Build();
    }

    public IAmazonDynamoDB Client { get; }
    public IDynamoDBContext Context { get; }

    public string ConnectionString { get; }
    public string TablePrefix { get; }

    public static async Task<DynamoDb> Create(CancellationToken token)
    {
        var containerBuilder = new DynamoDbBuilder();

        #if DEBUG
        // On developer machines, leave the container running for faster testing
        containerBuilder.WithName("bitsandbobs-dynamodb-test").WithReuse(true).WithAutoRemove(false);
        #endif

        var container = containerBuilder.Build();
        await container.StartAsync(token);

        return new DynamoDb(container);
    }

    public async Task<Table> CreateTableForCloudFormationResource(
        string resourceName,
        string cfnTemplatePath,
        string? tableName = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!Path.Exists(cfnTemplatePath))
            throw new FileNotFoundException($"CloudFormation template not found: {cfnTemplatePath}");

        var yaml = await File.ReadAllTextAsync(cfnTemplatePath, cancellationToken);
        var deserializer = new DeserializerBuilder().WithTagMapping(new TagName("!Sub"), typeof(string))
                                                    .WithTagMapping(new TagName("!Ref"), typeof(string))
                                                    .IgnoreUnmatchedProperties()
                                                    .Build();

        var template = deserializer.Deserialize<Dictionary<string, dynamic>>(yaml);
        var resources = (Dictionary<object, object>)template["Resources"];

        if (!resources.ContainsKey(resourceName))
            throw new KeyNotFoundException($"Resource '{resourceName}' not found in CloudFormation template.");

        var resource = (Dictionary<object, object>)resources[resourceName];

        if (!resource.TryGetValue("Type", out var type) || type.ToString() != "AWS::DynamoDB::Table")
            throw new InvalidOperationException($"Resource '{resourceName}' is not a DynamoDB table.");

        var properties = (Dictionary<object, object>)resource["Properties"];

        var request = new CreateTableRequest
        {
            TableName = TablePrefix + (tableName ?? (string)properties["TableName"]),
            BillingMode = (string)properties["BillingMode"],
            AttributeDefinitions = GetAttributeDefinitions(properties),
            KeySchema = GetKeySchema(properties),
            GlobalSecondaryIndexes = GetGlobalSecondaryIndexes(properties),
        };

        return await Table.Create(request, this, cancellationToken);
    }

    private static List<AttributeDefinition> GetAttributeDefinitions(Dictionary<object, object> properties)
    {
        var attributes = (List<object>)properties["AttributeDefinitions"];
        return attributes.Select(attr =>
                             {
                                 var attrDict = (Dictionary<object, object>)attr;
                                 return new AttributeDefinition
                                 {
                                     AttributeName = (string)attrDict["AttributeName"],
                                     AttributeType = (string)attrDict["AttributeType"]
                                 };
                             }
                         )
                         .ToList();
    }

    private static List<KeySchemaElement> GetKeySchema(Dictionary<object, object> properties)
    {
        var keySchema = (List<object>)properties["KeySchema"];
        return keySchema.Select(key =>
                            {
                                var keyDict = (Dictionary<object, object>)key;
                                return new KeySchemaElement
                                {
                                    AttributeName = (string)keyDict["AttributeName"],
                                    KeyType = (string)keyDict["KeyType"]
                                };
                            }
                        )
                        .ToList();
    }

    private static List<GlobalSecondaryIndex>? GetGlobalSecondaryIndexes(Dictionary<object, object> properties)
    {
        if (!properties.TryGetValue("GlobalSecondaryIndexes", out var gsiObj))
            return null;

        var gsiList = (List<object>)gsiObj;
        return gsiList.Select(gsi =>
                          {
                              var gsiDict = (Dictionary<object, object>)gsi;
                              return new GlobalSecondaryIndex
                              {
                                  IndexName = (string)gsiDict["IndexName"],
                                  KeySchema = ((List<object>)gsiDict["KeySchema"]).Select(key =>
                                          {
                                              var keyDict = (Dictionary<object, object>)key;
                                              return new KeySchemaElement
                                              {
                                                  AttributeName = (string)keyDict["AttributeName"],
                                                  KeyType = (string)keyDict["KeyType"]
                                              };
                                          }
                                      )
                                      .ToList(),
                                  Projection = new Projection
                                  {
                                      ProjectionType =
                                          (string)((Dictionary<object, object>)gsiDict["Projection"])[
                                              "ProjectionType"]
                                  },
                              };
                          }
                      )
                      .ToList();
    }

    public ValueTask DisposeAsync()
    {
        Context.Dispose();
        Client.Dispose();
        return _container.DisposeAsync();
    }

    public sealed class Table(string name, string namePrefix, DynamoDb dynamo) : IAsyncDisposable
    {
        /// <summary>
        /// The table name including an prefix
        /// </summary>
        public string FullName { get; } = $"{namePrefix}{name}";

        /// <summary>
        /// The table name (as it is on the attributes of the model classes
        /// </summary>
        public string Name { get; } = name;
        public DynamoDb Dynamo { get; } = dynamo;

        public static async Task<Table> Create(
            CreateTableRequest request,
            DynamoDb dynamoDb,
            CancellationToken cancellationToken = default
        )
        {
            await dynamoDb.Client.CreateTableAsync(request, cancellationToken);



            return new Table(request.TableName.Replace(dynamoDb.TablePrefix, ""), dynamoDb.TablePrefix, dynamoDb);
        }

        public async ValueTask DisposeAsync() =>
            await Dynamo.Client.DeleteTableAsync(Dynamo.TablePrefix + Name);
    }
}
