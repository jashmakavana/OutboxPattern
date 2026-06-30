namespace OutboxPattern.Api.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── DTOs ───────────────────────────────────────────────────────────────────

public record CreateOrderRequest(
    string CustomerEmail,
    string ProductName,
    decimal Amount
);

// ─── Domain Events (what goes into the Outbox payload) ───────────────────────

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerEmail,
    string ProductName,
    decimal Amount,
    DateTime CreatedAt
);
