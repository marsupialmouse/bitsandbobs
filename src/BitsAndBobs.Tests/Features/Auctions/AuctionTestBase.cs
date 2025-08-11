using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Tests.Features.Auctions;

public class AuctionTestBase : TestBase
{
    protected static Task<Auction?> GetAuctionFromDb(string id) =>
        GetAuctionFromDb(AuctionId.Parse(id));

    protected static Task<Auction?> GetAuctionFromDb(AuctionId id) =>
        DynamoContext.LoadAsync<Auction>(id.Value, Auction.SortKey)!;

    protected static async Task<Auction> CreateAuction(
        User? seller = null,
        string name = "Slightly used horse",
        string description = "Its mane is mainly mange",
        decimal initialPrice = 100m,
        decimal bidIncrement = 10m,
        DateTimeOffset? endDate = null,
        Action<Auction>? configure = null
    )
    {
        seller ??= new User { DisplayName = "Puffy Dog" };
        var image = new AuctionImage(".jpg", seller.Id);
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

    protected static Task AddBidToAuction(Auction auction, UserId bidderId, decimal amount) =>
        AddBidsToAuction(auction, (bidderId, amount));

    protected static async Task<IReadOnlyList<Bid>> AddBidsToAuction(Auction auction, params (UserId bidderId, decimal amount)[] bids)
    {
        var addedBids = bids.Select(bid => auction.AddBid(bid.bidderId, bid.amount)).ToList();

        await DynamoContext.SaveItem(auction);
        await Task.WhenAll(addedBids.Select(bid => DynamoContext.SaveItem(bid)));

        return addedBids;
    }
}
