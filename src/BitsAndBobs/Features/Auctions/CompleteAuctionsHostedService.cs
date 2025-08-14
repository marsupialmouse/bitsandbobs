using BitsAndBobs.Features.Auctions.Diagnostics;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Auctions;

/// <summary>
/// A background service to complete auctions that have finished but are still open.
/// </summary>
/// <remarks>
/// Ideally, we'd split this out into a separate app and run it as a single instance. Since this is not real, we have
/// instead opted for a naive distributed locking mechanism to ensure that we only check for completed auctions once a
/// minute, regardless of the number of instances. This naive lock assumes the periodic task will always finish within
/// the period.
/// </remarks>
public partial class CompleteAuctionsHostedService(
    IDistributedLockClient lockClient,
    ILogger<CompleteAuctionsHostedService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private const string LockName = "CompleteAuctions";

    /// <summary>
    /// Gets the number of loop iterations completed. This is only useful for testing.
    /// </summary>
    public int NumberOfIterations { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var diagnostics = new CompleteAuctionsDiagnostics(true);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        IDistributedLock? distributedLock = null;

        try
        {
            do
            {
                try
                {
                    distributedLock = await lockClient.TryAcquireLock(LockName, TimeSpan.FromSeconds(62));

                    if (distributedLock is null)
                        continue;

                    await CompleteAuctions(diagnostics, stoppingToken);
                }
                catch (Exception e)
                {
                    Log.FindingFinishedAuctionsFailed(logger, e);
                }

                NumberOfIterations++;

            } while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        }
        finally
        {
            if (distributedLock is { IsActive: true })
                await distributedLock.Release();
        }
    }

    private async Task CompleteAuctions(CompleteAuctionsDiagnostics diagnostics, CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var auctionService = scope.ServiceProvider.GetRequiredService<AuctionService>();
        var auctions = await auctionService.GetAuctionsForCompletion();

        foreach (var auction in auctions)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                await auctionService.CompleteAuction(auction);
                diagnostics.Completed(auction);
            }
            catch (Exception e)
            {
                Log.AuctionCompletionFailed(logger, e);
                diagnostics.FailedToComplete(auction, e);
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Message = "Finding auctions for completion failed", Level = LogLevel.Error)]
        public static partial void FindingFinishedAuctionsFailed(ILogger logger, Exception e);

        [LoggerMessage(EventId = 2, Message = "Failed to complete auction", Level = LogLevel.Error)]
        public static partial void AuctionCompletionFailed(ILogger logger, Exception e);
    }
}

