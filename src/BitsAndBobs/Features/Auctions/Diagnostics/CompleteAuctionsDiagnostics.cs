using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct CompleteAuctionsDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalCompletedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.complete.succeeded.total",
        description: "Number of completed auctions"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.complete.failed.total",
        description: "Number of failed failed auction completions"
    );

    private readonly Activity? _activity;

    // ReSharper disable once UnusedParameter.Local (it's there so we can create the activity)
    public CompleteAuctionsDiagnostics(bool nothing)
    {
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();
    }

    public void Completed(Auction auction)
    {
        if (_activity is { IsAllDataRequested: true })
            _activity.SetTag("auction.id", auction.Id.Value);

        TotalCompletedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("auction.completed"));
    }

    public void FailedToComplete(Auction auction, Exception? e = null)
    {
        if (_activity is { IsAllDataRequested: true })
            _activity.SetTag("auction.id", auction.Id.Value);

        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("auction.complete_failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
