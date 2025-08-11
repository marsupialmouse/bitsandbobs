using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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

    public async Task<Auction> CreateAuction(
        User seller,
        string name,
        string description,
        AuctionImageId imageId,
        decimal initialPrice,
        decimal bidIncrement,
        TimeSpan period
    )
    {
        var image = await _dynamoContext.LoadAsync<AuctionImage>(imageId.Value, AuctionImage.SortKey);

        if (image == null || image.UserId != seller.Id)
            throw new ImageNotFoundException();

        var auction = new Auction(seller, name, description, image, initialPrice, bidIncrement, period);

        try
        {
            var items = new List<TransactWriteItem>
            {
                new() { Put = _dynamoContext.CreateInsertPut(auction) },
                new() { Put = _dynamoContext.CreateUpdatePut(image) },
            };

            await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });

            return auction;
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
        _dynamoContext.LoadAsync<Auction>(id.Value, Auction.SortKey)!;

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

    /// <summary>
    /// Gets all active auctions
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Auction>> GetActiveAuctions()
    {
        var search = _dynamoContext.QueryAsync<Auction>(
            AuctionStatus.Open,
            QueryOperator.GreaterThan,
            [DateTimeOffset.Now.UtcTicks],
            new QueryConfig { IndexName = "AuctionsByStatus" }
        );

        return await search.GetRemainingAsync();
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
                new() { Put = _dynamoContext.CreateUpsertPut(new UserAuctionBid(bid)) },
            };

            await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });

            return bid;
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            throw new DynamoDbConcurrencyException();
        }
    }

    public async Task CancelAuction(Auction auction, UserId userId)
    {
        if (userId != auction.SellerId)
            throw new InvalidOperationException("An auction can only be cancelled by the seller");

        auction.Cancel();

        try
        {
            var items = new List<TransactWriteItem>
            {
                new() { Put = _dynamoContext.CreateUpdatePut(auction) },
            };

            // Use TransactWriteItems so we can add the Put conditions (see above)
            await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items });
        }
        catch (TransactionCanceledException e) when (e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            throw new DynamoDbConcurrencyException();
        }
    }

    /// <summary>
    /// Gets all auctions belonging to the user
    /// </summary>
    public async Task<IEnumerable<Auction>> GetUserAuctions(UserId userId)
    {
        var search = _dynamoContext.QueryAsync<Auction>(
            userId,
            new QueryConfig { IndexName = "AuctionsBySeller" }
        );

        return await search.GetRemainingAsync();
    }

    /// <summary>
    /// Gets all auctions won by the user
    /// </summary>
    public async Task<IEnumerable<Auction>> GetWonAuctions(UserId userId)
    {
        var search = _dynamoContext.QueryAsync<Auction>(
            userId,
            QueryOperator.Equal,
            [AuctionStatus.Complete],
            new QueryConfig { IndexName = "AuctionsByCurrentBidder" }
        );

        return await search.GetRemainingAsync();
    }

    /// <summary>
    /// Gets all auctions belonging to the user
    /// </summary>
    public async Task<IEnumerable<UserAuctionParticipation>> GetUserAuctionParticipation(UserId userId)
    {
        var bidsQuery = _dynamoContext.QueryAsync<UserAuctionBid>(
            userId.Value,
            new QueryConfig { IndexName = "UserAuctionBidsByDate", BackwardQuery = true }
        );
        var bids = (await bidsQuery.GetRemainingAsync()).ToDictionary(x => x.AuctionId);

        var batch = _dynamoContext.CreateBatchGet<Auction>();

        foreach (var item in bids)
            batch.AddKey(item.Value.AuctionId.Value, Auction.SortKey);

        await _dynamoContext.ExecuteBatchGetAsync(batch);

        return batch.Results.Select(x => UserAuctionParticipation.Create(x, bids[x.Id]));
    }
}

public record UserAuctionParticipation(UserId UserId, Auction Auction, DateTimeOffset LastBidDate, decimal MaximumBid)
{
    public static UserAuctionParticipation Create(Auction auction, UserAuctionBid bid) =>
        new(bid.UserId, auction, bid.LastBidDate, bid.Amount);
}

public class ImageNotFoundException : Exception
{
}
