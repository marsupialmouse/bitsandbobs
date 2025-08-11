using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct GetAuctionDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalSucceededCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_one_requests.succeeded.total",
        description: "Number of successful get auction requests"
    );

    private static readonly Counter<int> TotalRequestCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_one_requests.total",
        description: "Number of get auction requests"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.get_one_requests.failed.total",
        description: "Number of failed get auction requests"
    );

    private static readonly Histogram<double> TotalDuration = BitsAndBobsDiagnostics.Meter.CreateHistogram<double>(
        "bitsandbobs.api.auctions.get_one_requests.duration.seconds",
        description: "The time spent handling get auction requests"
    );

    private readonly Activity? _activity;
    private readonly BitsAndBobsDiagnostics.ValueStopwatch? _stopwatch;

    public GetAuctionDiagnostics(AuctionId auctionId)
    {
        _stopwatch = BitsAndBobsDiagnostics.ValueStopwatch.StartNew();
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);

        if (_activity is { IsAllDataRequested: true })
            _activity.SetTag("auction.id", auctionId.Value);
    }

    private GetAuctionDiagnostics(string auctionId)
    {
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);

        if (_activity is { IsAllDataRequested: true })
            _activity.SetTag("auction.id", auctionId);
    }

    public static void InvalidId(string id)
    {
        using var diagnostics = new GetAuctionDiagnostics(id);

        diagnostics._activity?.AddEvent(new ActivityEvent("auction.invalid_id"));
        diagnostics.Failed();
    }

    public void Succeeded()
    {
        TotalSucceededCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("get_auction.succeeded"));
    }

    public void NotFound()
    {
        _activity?.AddEvent(new ActivityEvent("auction.not_found"));
        Failed();
    }

    public void Failed(Exception? e = null)
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("get_auction.failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Dispose()
    {
        if (_stopwatch.HasValue)
            TotalDuration.Record(_stopwatch.Value.Elapsed.TotalSeconds);

        _activity?.Dispose();
    }
}
