using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Testing.Platform.Services;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Endpoints;

[TestFixture]
public class CompleteAuctionsServiceTest : AuctionTestBase
{
    [Test]
    public async Task ShouldCompleteOpenAuctionsThatHaveEnded()
    {
        var openAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(10));
        var endedAuction1 = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(-1));
        var cancelledAuction = await CreateAuction();
        var endedAuction2 = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(-1));
        await UpdateStatus(cancelledAuction, AuctionStatus.Cancelled, DateTimeOffset.Now.AddMinutes(-20));

        await RunServiceForOneIteration();

        (await GetAuctionFromDb(openAuction))!.Status.ShouldBe(AuctionStatus.Open);
        (await GetAuctionFromDb(cancelledAuction))!.Status.ShouldBe(AuctionStatus.Cancelled);
        (await GetAuctionFromDb(endedAuction1))!.Status.ShouldBe(AuctionStatus.Complete);
        (await GetAuctionFromDb(endedAuction2))!.Status.ShouldBe(AuctionStatus.Complete);
    }

    [Test]
    public async Task ShouldPublishEventsForCompletedAuctions()
    {
        var endedAuction1 = await CreateAuction(endDate: DateTimeOffset.Now.AddHours(-1));
        var endedAuction2 = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(-1));
        var openAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(10));
        var completeAuction = await CreateAuction(endDate: DateTimeOffset.Now.AddMinutes(-25), configure: a => a.Complete());
        var cancelledAuction = await CreateAuction();
        await UpdateStatus(cancelledAuction, AuctionStatus.Cancelled, DateTimeOffset.Now.AddMinutes(-20));
        var validIds = new HashSet<string>(
            new[] { endedAuction1, endedAuction2, openAuction, completeAuction, cancelledAuction }
                .Select(x => x.Id.Value)
        );

        await RunServiceForOneIteration();

        // We filter to the IDs created in this test to ignore auctions created in other tests
        var events = Messaging.Published.Select<AuctionCompleted>().Select(x => x.Context.Message.AuctionId).Where(validIds.Contains).ToList();
        events.ShouldBe([endedAuction1.Id.Value, endedAuction2.Id.Value], ignoreOrder: true);
    }

    private async Task RunServiceForOneIteration()
    {
        AppFactory.ConfiguringServices += s => s.TryAddTransient<CompleteAuctionsService>();

        var service = AppFactory.Services.GetRequiredService<CompleteAuctionsService>();
        await service.StartAsync(CancellationToken.None);

        var waitTime = 0;

        while (service.NumberOfIterations < 1)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20));
            waitTime += 20;

            if (waitTime >= 5000)
                throw new TimeoutException("This thing is taking too long");
        }

        await service.StopAsync(CancellationToken.None);
    }
}
