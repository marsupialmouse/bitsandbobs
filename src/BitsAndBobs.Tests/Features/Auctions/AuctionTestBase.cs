using Amazon.DynamoDBv2.Model;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Tests.Features.Auctions;

public class AuctionTestBase : TestBase
{
    protected static Task<Auction?> GetAuctionFromDb(string id) =>
        GetAuctionFromDb(AuctionId.Parse(id));

    protected static Task<Auction?> GetAuctionFromDb(Auction auction) =>
        GetAuctionFromDb(auction.Id);

    protected static Task<Auction?> GetAuctionFromDb(AuctionId id) =>
        DynamoContext.LoadAsync<Auction>(id.Value, Auction.SortKey)!;

    protected static Task<List<Bid>> GetBidsFromDb(Auction auction) => GetBidsFromDb(auction.Id);

    protected static async Task<List<Bid>> GetBidsFromDb(AuctionId id)
    {
        var items = await DynamoContext.QueryAsync<BitsAndBobsTable.Item>(id.Value).GetRemainingAsync();
        return items.OfType<Bid>().ToList();
    }

    protected static async Task<Auction> CreateAuction(
        User? seller = null,
        string name = "Slightly used horse",
        string description = "Its mane is mainly mange",
        string imageExtension = ".jpg",
        decimal initialPrice = 100m,
        decimal bidIncrement = 10m,
        DateTimeOffset? endDate = null,
        Action<Auction>? configure = null
    )
    {
        seller ??= new User { DisplayName = "Puffy Dog" };
        var image = new AuctionImage(imageExtension, seller.Id);
        var auction = new Auction(
            seller,
            name,
            description,
            image,
            initialPrice,
            bidIncrement,
            endDate?.Subtract(DateTimeOffset.Now) ?? TimeSpan.FromHours(1)
        );
        configure?.Invoke(auction);
        await DynamoContext.SaveItem(auction);
        return auction;
    }

    protected static async Task<Bid> AddBidToAuction(Auction auction, UserId bidderId, decimal amount) =>
        (await AddBidsToAuction(auction, (bidderId, amount)))[0];

    protected static async Task<IReadOnlyList<Bid>> AddBidsToAuction(Auction auction, params (UserId bidderId, decimal amount)[] bids)
    {
        var addedBids = bids.Select(bid => auction.AddBid(bid.bidderId, bid.amount)).ToList();

        await DynamoContext.SaveItem(auction);
        await Task.WhenAll(addedBids.Select(bid => DynamoContext.SaveItem(bid)));

        return addedBids;
    }

    protected Task UpdateStatus(Auction auction, AuctionStatus status, DateTimeOffset? endDate = null) =>
        UpdateStatus((auction, status, endDate ?? auction.EndDate));

    protected Task UpdateStatus(params (Auction auction, AuctionStatus status, DateTimeOffset endDate)[] auctions)
    {
        return Testing.DynamoClient.TransactWriteItemsAsync(
            new TransactWriteItemsRequest
            {
                TransactItems = auctions.Select(x => UpdateItem(x.auction, x.status, x.endDate)).ToList()
            }
        );

        static TransactWriteItem UpdateItem(Auction a, AuctionStatus s, DateTimeOffset d) => new()
        {
            Update = new Update
            {
                TableName = BitsAndBobsTable.FullName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue(a.Id.Value) },
                    { "SK", new AttributeValue(Auction.SortKey) },
                },
                UpdateExpression = "SET AuctionStatus = :status, EndDate = :endDate",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":status", new AttributeValue { N = ((int)s).ToString() } },
                    { ":endDate", new AttributeValue(d.ToString("O")) },
                },
            }
        };
    }
}
