namespace OutboxPattern.Api.Services;

/// <summary>
/// In a real system this would be:
///   - RabbitMQ  (using MassTransit / EasyNetQ)
///   - Azure Service Bus
///   - AWS SNS / SQS
///   - Apache Kafka
///
/// Here we simulate it with a simple in-memory log so you can see
/// exactly what gets "published" without needing a running broker.
/// </summary>
public interface IMessageBroker
{
    Task PublishAsync(string eventType, string payload, CancellationToken ct = default);
    IReadOnlyList<PublishedMessage> GetPublishedMessages();
}

public record PublishedMessage(string EventType, string Payload, DateTime PublishedAt);

public class FakeMessageBroker : IMessageBroker
{
    private readonly List<PublishedMessage> _published = new();
    private readonly ILogger<FakeMessageBroker> _logger;

    public FakeMessageBroker(ILogger<FakeMessageBroker> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string eventType, string payload, CancellationToken ct = default)
    {
        // Simulate occasional network failure (1-in-5 chance) to show retry logic
        if (Random.Shared.Next(5) == 0)
        {
            _logger.LogWarning("[Broker] Simulated transient failure for {EventType}", eventType);
            throw new InvalidOperationException("Simulated broker transient failure");
        }

        var msg = new PublishedMessage(eventType, payload, DateTime.UtcNow);
        _published.Add(msg);

        _logger.LogInformation(
            "[Broker] ✅ Published event '{EventType}' at {Time}",
            eventType, msg.PublishedAt);

        return Task.CompletedTask;
    }

    public IReadOnlyList<PublishedMessage> GetPublishedMessages() => _published.AsReadOnly();
}
