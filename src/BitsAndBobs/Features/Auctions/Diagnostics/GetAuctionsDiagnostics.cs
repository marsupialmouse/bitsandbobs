using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct GetAuctionsDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalSucceededCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_many_requests.succeeded.total",
        description: "Number of successful get auction requests"
    );

    private static readonly Counter<int> TotalRequestCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_many_requests.total",
        description: "Number of get auction requests"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_many_requests.failed.total",
        description: "Number of failed get auction requests"
    );

    private static readonly Histogram<double> TotalDuration = BitsAndBobsDiagnostics.Meter.CreateHistogram<double>(
        "bitsandbobs.api.auctions.get_many_requests.duration.seconds",
        description: "The time spent handling get auction requests"
    );

    private readonly Activity? _activity;
    private readonly BitsAndBobsDiagnostics.ValueStopwatch _stopwatch;

    public GetAuctionsDiagnostics()
    {
        _stopwatch = BitsAndBobsDiagnostics.ValueStopwatch.StartNew();
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);
    }

    public void Succeeded()
    {
        TotalSucceededCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("get_auctions.succeeded"));
    }

    public void Failed(Exception? e = null)
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("get_actions.failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Dispose()
    {
        TotalDuration.Record(_stopwatch.Elapsed.TotalSeconds);
        _activity?.Dispose();
    }
}
