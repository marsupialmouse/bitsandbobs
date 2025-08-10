using System.Diagnostics;
using System.Diagnostics.Metrics;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct BidDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalAcceptedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.bid_requests.accepted.total",
        description: "Number of accepted auction bid requests"
    );

    private static readonly Counter<int> TotalRequestCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.bid_requests.total",
        description: "Number of auction bid requests"
    );

    private static readonly Counter<int> TotalInvalidCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.bid_requests.invalid.total",
        description: "Number of invalid auction bid requests"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.bid_requests.failed.total",
        description: "Number of failed auction bid requests"
    );

    private readonly Activity? _activity;

    public BidDiagnostics(AuctionId auction, UserId user, decimal amount)
    {
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);

        if (_activity is { IsAllDataRequested: true })
        {
            _activity.SetTag("auction.id", auction.Value);
            _activity.SetTag("user_id", user.Value);
            _activity.SetTag("bid.amount", amount.ToString("F2"));
        }
    }

    public void AddAuctionDetails(Auction auction)
    {
        if (_activity is { IsAllDataRequested: true })
        {
            _activity.SetTag("auction.current_price", auction.CurrentPrice.ToString("F2"));
            _activity.SetTag("auction.current_bid.id", auction.CurrentBidId);
            _activity.SetTag("auction.current_bid.user.id", auction.CurrentBidderId?.Value);
        }
    }

    public void Accepted()
    {
        TotalAcceptedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("bid.accepted"));
    }

    public void AuctionNotFound()
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("auction.not_found"));
    }

    public void Failed(Exception? e = null)
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("bid.failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Invalid()
    {
        TotalInvalidCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("bid.invalid"));
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
