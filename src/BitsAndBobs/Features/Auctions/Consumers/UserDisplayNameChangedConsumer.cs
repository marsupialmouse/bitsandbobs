using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;
using MassTransit;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Auctions.Consumers;

// ReSharper disable once ClassNeverInstantiated.Global
public class UserDisplayNameChangedConsumer(IUserStore<User> userStore, IAmazonDynamoDB dynamo) : IConsumer<UserDisplayNameChanged>
{
    public async Task Consume(ConsumeContext<UserDisplayNameChanged> context)
    {
        var message = context.Message;
        var user = (await userStore.FindByIdAsync(message.UserId, context.CancellationToken))
                   ?? throw new ArgumentException("User not found");

        if (user.DisplayName != message.NewDisplayName)
            throw new ArgumentException("NewDisplayName does not match current DisplayName");

        while (true)
        {
            var auctionsIds = await GetAuctionsToUpdate(message, context.CancellationToken);

            if (auctionsIds.Count == 0)
                return;

            await UpdateAuctions(auctionsIds, user, message, context);

            // We retrieve up to 100, but only update 99 at a time (transaction limit is 100, but we also include the
            // user check), so if there are 100 IDs we know there's at least one more auction that needs updating.
            if (auctionsIds.Count < 100)
                return;
        }
    }

    private async Task<List<string>> GetAuctionsToUpdate(UserDisplayNameChanged message, CancellationToken cancellationToken)
    {
        var queryResult = await dynamo.QueryAsync(
                              new QueryRequest
                              {
                                  ExpressionAttributeValues =
                                      new Dictionary<string, AttributeValue>
                                      {
                                          {
                                              ":displayName", new AttributeValue(message.NewDisplayName)
                                          },
                                          { ":sellerId", new AttributeValue(message.UserId) },
                                      },
                                  FilterExpression = "SellerDisplayName <> :displayName",
                                  IndexName = "AuctionsBySeller",
                                  KeyConditionExpression = "SellerId = :sellerId",
                                  Limit = 100,
                                  ProjectionExpression = "PK",
                                  TableName = BitsAndBobsTable.FullName,
                              },
                              cancellationToken
                          );

        return queryResult.Items.Select(x => x["PK"].S).ToList();
    }

    private async Task UpdateAuctions(List<string> auctionsIds, User seller, UserDisplayNameChanged message, ConsumeContext<UserDisplayNameChanged> context)
    {
        var transactItems = (from auctionId in auctionsIds.Take(99)
                             select CreateAuctionUpdate(auctionId, message)).ToList();

        transactItems.Add(CreateUserCheck(seller, message));

        try
        {
            await dynamo.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = transactItems },
                context.CancellationToken
            );
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            throw new DynamoDbConcurrencyException("User has been updated");
        }
        catch (ConditionalCheckFailedException)
        {
            throw new DynamoDbConcurrencyException("User has been updated");
        }
    }

    /// <summary>
    /// Creates a <see cref="TransactWriteItem"/> that updates <see cref="Auction.SellerDisplayName"/>. Note that we
    /// don't check the version on the auction, since we know we're dealing with the current display name and we're
    /// not changing any other values.
    /// </summary>
    private static TransactWriteItem CreateAuctionUpdate(string auctionId, UserDisplayNameChanged message) =>
        new()
        {
            Update = new Update
            {
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue(auctionId) },
                    { "SK", new AttributeValue(Auction.SortKey) },
                },
                UpdateExpression = "SET SellerDisplayName = :displayName, Version = :version",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":displayName", new AttributeValue(message.NewDisplayName) },
                    { ":version", new AttributeValue(BitsAndBobsTable.VersionedEntity.NewVersion()) },
                },
                TableName = BitsAndBobsTable.FullName,
            },
        };

    /// <summary>
    /// Creates a <see cref="TransactWriteItem"/> that does a fake update on the user, but with a condition expression
    /// that checks that the version and DisplayName are unchanged. By including this in the auction update transaction
    /// we can be sure that we're updating auctions with the latest seller name.
    /// </summary>
    private static TransactWriteItem CreateUserCheck(User user, UserDisplayNameChanged message) =>
        new()
        {
            Update = new Update
            {
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue(message.UserId) },
                    { "SK", new AttributeValue(User.SortKey) },
                },
                ConditionExpression =
                    "attribute_exists(PK) AND attribute_exists(SK) AND Version = :currentVersion AND DisplayName = :displayName",
                UpdateExpression = "SET DisplayName = :displayName",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":displayName", new AttributeValue(message.NewDisplayName) },
                    { ":currentVersion", new AttributeValue(user.Version) },
                },
                TableName = BitsAndBobsTable.FullName,
            },
        };
}
