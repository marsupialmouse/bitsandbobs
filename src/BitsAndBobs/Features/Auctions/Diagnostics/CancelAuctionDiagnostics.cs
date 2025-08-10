using System.Diagnostics;
using System.Diagnostics.Metrics;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct CancelAuctionDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalCancelledCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.cancel_requests.cancelled.total",
        description: "Number of cancelled auctions"
    );

    private static readonly Counter<int> TotalRequestCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.cancel_requests.total",
        description: "Number of cancel auction requests"
    );

    private static readonly Counter<int> TotalInvalidCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.cancel_requests.invalid.total",
        description: "Number of invalid cancel auction requests"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.cancel_requests.failed.total",
        description: "Number of failed cancel auction requests"
    );

    private readonly Activity? _activity;

    public CancelAuctionDiagnostics(AuctionId auction, UserId user)
    {
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);

        if (_activity is { IsAllDataRequested: true })
        {
            _activity.SetTag("auction.id", auction.Value);
            _activity.SetTag("user.id", user.Value);
        }
    }

    public void Cancelled()
    {
        TotalCancelledCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("cancel_action.cancelled"));
    }

    public void AuctionNotFound()
    {
        _activity?.AddEvent(new ActivityEvent("auction.not_found"));
        Failed();
    }

    public void Failed(Exception? e = null)
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("cancel_action.failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Invalid()
    {
        TotalInvalidCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("cancel_action.invalid"));
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
