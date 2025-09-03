using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Mcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Mcp;

public class GetAuctionToolTest : AuctionTestBase
{
    [Test]
    public async Task ShouldGetAuctionDetails()
    {
        var user = await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(name: "Adam Sandler's Shorts", seller: user);

        var result = await McpClient.CallToolAsync(
                         "get_auction",
                         cancellationToken: TestContext.CurrentContext.CancellationToken,
                         arguments: new Dictionary<string, object?> { ["id"] = expectedAuction.Id.FriendlyValue }
                     );

        result.IsError.GetValueOrDefault().ShouldBeFalse();
        var auction = result.GetStructuredContent<GetAuctionTool.GetAuctionResponse>();
        auction.Id.ShouldBe(expectedAuction.Id.FriendlyValue);
        auction.Name.ShouldBe(expectedAuction.Name);
        auction.Description.ShouldBe(expectedAuction.Description);
        auction.InitialPrice.ShouldBe(expectedAuction.InitialPrice);
        auction.CurrentPrice.ShouldBe(expectedAuction.CurrentPrice);
        auction.MinimumBid.ShouldBe(expectedAuction.MinimumBid);
        auction.EndDate.ShouldBe(expectedAuction.EndDate);
        auction.NumberOfBids.ShouldBe(expectedAuction.NumberOfBids);
        auction.SellerDisplayName.ShouldBe(expectedAuction.SellerDisplayName);
        auction.IsOpen.ShouldBeTrue();
        auction.IsClosed.ShouldBeFalse();
        auction.IsCancelled.ShouldBeFalse();
        auction.IsUserSeller.ShouldBeTrue();
        auction.IsUserCurrentBidder.ShouldBeFalse();
        auction.CurrentBidderDisplayName.ShouldBeNull();
        auction.Bids.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReturnErrorWhenAuctionNotFound()
    {
        await CreateAuthenticatedUser();

        var result = await McpClient.CallToolAsync(
                         "get_auction",
                         cancellationToken: TestContext.CurrentContext.CancellationToken,
                         arguments: new Dictionary<string, object?> { ["id"] = AuctionId.Create().FriendlyValue }
                     );

        result.IsError.GetValueOrDefault().ShouldBeTrue();
        result.Content[0].Type.ShouldBe("text");
        ((TextContentBlock) result.Content[0]).Text.ShouldContain("Auction not found");
    }
}
