namespace OutboxPattern.Api.Models;

/// <summary>
/// Represents a message stored in the Outbox table.
/// The Outbox pattern guarantees at-least-once delivery by persisting
/// messages in the same DB transaction as the business operation.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Event type name (e.g. "OrderCreated", "PaymentProcessed")</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON-serialised event payload</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>When this message was added to the outbox</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null = not yet processed; set when a relay worker picks it up</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Last error, if any attempt failed</summary>
    public string? Error { get; set; }

    /// <summary>How many times processing has been retried</summary>
    public int RetryCount { get; set; } = 0;

    public bool IsProcessed => ProcessedAt.HasValue;
}
