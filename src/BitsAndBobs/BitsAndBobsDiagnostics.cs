using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BitsAndBobs;

internal static class BitsAndBobsDiagnostics
{
    private const string ServiceName = "BitsAndBobs.Api";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static void AddErrorEvent(this Activity activity, Exception exception) =>
        activity.AddEvent(
            new ActivityEvent(
                "Error",
                tags: new ActivityTagsCollection
                {
                    { "event", "error" },
                    { "error.object", exception },
                    { "error.kind", exception.GetBaseException().GetType().Name },
                    { "message", exception.GetBaseException().Message },
                }
            )
        );

    internal readonly struct ValueStopwatch
    {
        private readonly long _startingTimestamp;

        private ValueStopwatch(long startingTimestamp) => _startingTimestamp = startingTimestamp;

        public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startingTimestamp);

        public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
    }
}
