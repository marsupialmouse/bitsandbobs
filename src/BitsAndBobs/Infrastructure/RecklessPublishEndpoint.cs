using MassTransit;

namespace BitsAndBobs.Infrastructure;

/// <summary>
/// A thin wrapper around <see cref="IPublishEndpoint"/> that throws caution to the wind, catching exceptions and
/// hiding the - possibly awful - truth about the destiny of the event from the publisher.
/// </summary>
/// <remarks>
/// In the real world we'd use a transactional outbox, but that doesn't exist for DynamoDB, and I'm not interested in
/// writing one. So instead, we pretend that messages never fail to publish and accept that some events for our fake
/// auctions might not really be published.
/// </remarks>
public partial class RecklessPublishEndpoint(IPublishEndpoint endpoint, ILogger<RecklessPublishEndpoint> logger)
{
    /// <summary>
    /// Publishes an event and hides the truth from you about whether it succeeded. You should call this after saving things to the database.
    /// </summary>
    public async Task PublishRecklessly<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await endpoint.Publish(message, cancellationToken);
        }
        catch (Exception e)
        {
            Log.PublishingFailed(logger, typeof(T).FullName!, e);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Message = "Failed to publish event of type '{eventType}'", Level = LogLevel.Error)]
        public static partial void PublishingFailed(ILogger logger, string eventType, Exception e);
    }
}
