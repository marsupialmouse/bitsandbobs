using BitsAndBobs.Features.Auctions.Mcp;
using ModelContextProtocol.Client;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Mcp;

public class GetCurrentAuctionsToolTest : AuctionTestBase
{
    [Test]
    public async Task ShouldGetCurrentAuctions()
    {
        await CreateAuthenticatedUser();
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        await CreateAuction(name: $"{uniquePrefix}-Soon", endDate: DateTimeOffset.Now.AddMinutes(30));
        await CreateAuction(name: $"{uniquePrefix}-Later", endDate: DateTimeOffset.Now.AddHours(2));
        await CreateAuction(name: $"{uniquePrefix}-Closed", endDate: DateTimeOffset.Now.AddHours(-20), configure: a => a.Complete());
        await CreateAuction(name: $"{uniquePrefix}-Cancelled", endDate: DateTimeOffset.Now.AddHours(15), configure: a => a.Cancel());

        var result = await McpClient.CallToolAsync("get_current_auctions", cancellationToken: TestContext.CurrentContext.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeFalse();
        var response = result.GetStructuredContent<GetCurrentAuctionsTool.GetCurrentAuctionsResponse>();
        var auctions = response.Auctions.Where(a => a.Name.StartsWith(uniquePrefix)).ToList();
        auctions.Count.ShouldBe(2);
        auctions[0].Name.ShouldBe($"{uniquePrefix}-Soon");
        auctions[1].Name.ShouldBe($"{uniquePrefix}-Later");
    }

    [Test]
    public async Task ShouldReturnBasicAuctionDetails()
    {
        await CreateAuthenticatedUser();
        var expectedAuction = await CreateAuction(name: "Grown Ups 2");

        var result = await McpClient.CallToolAsync("get_current_auctions", cancellationToken: TestContext.CurrentContext.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeFalse();
        var response = result.GetStructuredContent<GetCurrentAuctionsTool.GetCurrentAuctionsResponse>();
        var auction = response.Auctions.FirstOrDefault(a => a.Id == expectedAuction.Id.FriendlyValue);
        auction.ShouldNotBeNull();
        auction.Name.ShouldBe(expectedAuction.Name);
        auction.Description.ShouldBe(expectedAuction.Description);
        auction.CurrentPrice.ShouldBe(expectedAuction.CurrentPrice);
        auction.EndDate.ShouldBe(expectedAuction.EndDate);
        auction.NumberOfBids.ShouldBe(expectedAuction.NumberOfBids);
        auction.SellerDisplayName.ShouldBe(expectedAuction.SellerDisplayName);
    }
}
