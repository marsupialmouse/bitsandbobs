using System.Diagnostics;
using System.Diagnostics.Metrics;
using BitsAndBobs.Features.Identity;

namespace BitsAndBobs.Features.Auctions.Diagnostics;

internal readonly struct CreateAuctionDiagnostics : IDisposable
{
    private static readonly Counter<int> TotalCreatedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.create_requests.created.total",
        description: "Number of created auctions"
    );

    private static readonly Counter<int> TotalRequestCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.create_requests.total",
        description: "Number of create auction requests"
    );

    private static readonly Counter<int> TotalFailedCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.create_requests.failed.total",
        description: "Number of failed create auction requests"
    );

    private static readonly Counter<int> TotalInvalidCount = BitsAndBobsDiagnostics.Meter.CreateCounter<int>(
        "bitsandbobs.api.auctions.create_requests.invalid.total",
        description: "Number of invalid create auction requests"
    );

    private readonly Activity? _activity;

    public CreateAuctionDiagnostics(AuctionImageId imageId, UserId user)
    {
        _activity = BitsAndBobsDiagnostics.ActivitySource.StartActivity();

        TotalRequestCount.Add(1);

        if (_activity is { IsAllDataRequested: true })
        {
            _activity.SetTag("auction_image.id", imageId.Value);
            _activity.SetTag("user.id", user.Value);
        }
    }

    public void Created(Auction auction)
    {
        if (_activity is { IsAllDataRequested: true })
            _activity.SetTag("auction.id", auction.Id.Value);

        TotalCreatedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("auction.created"));
    }

    public void ImageNotFound()
    {
        _activity?.AddEvent(new ActivityEvent("auction_image.not_found"));
        Invalid();
    }

    public void UserNotFound()
    {
        _activity?.AddEvent(new ActivityEvent("user.not_found"));
        Invalid();
    }

    public void Invalid()
    {
        TotalInvalidCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("create_action.invalid"));
    }

    public void Failed(Exception? e = null)
    {
        TotalFailedCount.Add(1);
        _activity?.AddEvent(new ActivityEvent("create_action.failed"));

        if (e is not null)
            _activity?.AddErrorEvent(e);
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
