using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Mcp;

public class AddBidToolTest : AuctionTestBase
{
    [Test]
    public async Task ShouldBeSuccessfulWhenAddingValidBid()
    {
        await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(initialPrice: 10m);

        var result = await McpClient.CallToolAsync(
                         "add_bid",
                         cancellationToken: TestContext.CurrentContext.CancellationToken,
                         arguments: new Dictionary<string, object?>
                         {
                             ["auctionId"] = expectedAuction.Id.FriendlyValue,
                             ["bidAmount"] = 87.34m,
                         }
                     );

        result.IsError.GetValueOrDefault().ShouldBeFalse();
    }

    [Test]
    public async Task ShouldAddBidToAuction()
    {
        var user = await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(initialPrice: 10m);

        await McpClient.CallToolAsync(
            "add_bid",
            cancellationToken: TestContext.CurrentContext.CancellationToken,
            arguments: new Dictionary<string, object?>
            {
                ["auctionId"] = expectedAuction.Id.FriendlyValue,
                ["bidAmount"] = 87.34m,
            }
        );

        var bids = await GetBidsFromDb(expectedAuction);
        bids.Count.ShouldBe(1);
        bids[0].Amount.ShouldBe(87.34m);
        bids[0].BidderId.ShouldBe(user.Id);
    }

    [Test]
    public async Task ShouldReturnErrorWhenAddingBidToOwnAuction()
    {
        var user = await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(seller: user, initialPrice: 10m);

        var result = await McpClient.CallToolAsync(
                         "add_bid",
                         cancellationToken: TestContext.CurrentContext.CancellationToken,
                         arguments: new Dictionary<string, object?>
                         {
                             ["auctionId"] = expectedAuction.Id.FriendlyValue,
                             ["bidAmount"] = 87.34m,
                         }
                     );

        result.IsError.GetValueOrDefault().ShouldBeTrue();
        result.Content[0].Type.ShouldBe("text");
        ((TextContentBlock) result.Content[0]).Text.ShouldContain("You cannot bid on your own auction");
    }

    [Test]
    public async Task ShouldReturnErrorWhenAddingBidLessTahnMinimumBid()
    {
        await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(initialPrice: 48m);

        var result = await McpClient.CallToolAsync(
                         "add_bid",
                         cancellationToken: TestContext.CurrentContext.CancellationToken,
                         arguments: new Dictionary<string, object?>
                         {
                             ["auctionId"] = expectedAuction.Id.FriendlyValue,
                             ["bidAmount"] = 10m,
                         }
                     );

        result.IsError.GetValueOrDefault().ShouldBeTrue();
        result.Content[0].Type.ShouldBe("text");
        ((TextContentBlock) result.Content[0]).Text.ShouldContain("Bid amount must be at least 48");
    }
}
