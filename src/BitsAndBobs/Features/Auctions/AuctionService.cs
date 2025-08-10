using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;

namespace BitsAndBobs.Features.Auctions;


public class AuctionService
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly IDynamoDBContext _dynamoContext;

    public AuctionService(IAmazonDynamoDB dynamo, IDynamoDBContext dynamoContext)
    {
        _dynamo = dynamo;
        _dynamoContext = dynamoContext;
    }

    /// <summary>
    /// Adds a bid to an auction and saves the updated values.
    /// </summary>
    public async Task<Bid> AddBid(Auction auction, UserId userId, decimal amount)
    {
        var bid = auction.AddBid(userId, amount);

        try
        {
            var items = new List<TransactWriteItem>
            {
                new() { Put = _dynamoContext.CreateUpdatePut(auction) },
                new() { Put = _dynamoContext.CreateInsertPut(bid) },
            };

            await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });

            return bid;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            throw new DynamoDbConcurrencyException();
        }
    }

    /// <summary>
    /// Gets an auction without loading bids.
    /// </summary>
    public Task<Auction?> GetAuction(AuctionId id) =>
        _dynamoContext.LoadAsync<Auction>(id, Auction.SortKey)!;

    /// <summary>
    /// Loads and auction and all of its bids.
    /// </summary>
    public async Task<Auction?> GetAuctionWithBids(AuctionId id)
    {
        var query = _dynamoContext.QueryAsync<BitsAndBobsTable.Item>(id.Value);
        var results = await query.GetRemainingAsync();

        Auction? auction = null;
        var bids = new List<Bid>();

        foreach (var item in results)
        {
            if (item is Auction a)
                auction = a;

            else if (item is Bid b)
                bids.Add(b);
        }

        if (auction is null)
            return null;

        auction.Bids = bids;

        return auction;
    }
}
