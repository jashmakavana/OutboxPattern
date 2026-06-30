using OutboxPattern.Api.Data;
using OutboxPattern.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace OutboxPattern.Api.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IOutboxService _outbox;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, IOutboxService outbox, ILogger<OrderService> logger)
    {
        _db      = db;
        _outbox  = outbox;
        _logger  = logger;
    }

    /// <summary>
    /// ┌─────────────────────────────────────────────────────────────┐
    /// │  SINGLE TRANSACTION:                                        │
    /// │  1. Insert Order row                                        │
    /// │  2. Insert OutboxMessage row (OrderCreatedEvent)            │
    /// │  3. SaveChanges  →  both rows commit atomically             │
    /// │                                                             │
    /// │  If the process crashes BEFORE step 3 → nothing saved.     │
    /// │  If it crashes AFTER step 3 → both rows are durable and    │
    /// │  the relay worker will pick up the outbox message.         │
    /// └─────────────────────────────────────────────────────────────┘
    /// </summary>
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        // 1. Create the business entity
        var order = new Order
        {
            Id            = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            ProductName   = request.ProductName,
            Amount        = request.Amount,
            Status        = "Pending",
            CreatedAt     = DateTime.UtcNow
        };

        await _db.Orders.AddAsync(order, ct);

        // 2. Write the domain event to the outbox (same unit-of-work)
        var domainEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerEmail,
            order.ProductName,
            order.Amount,
            order.CreatedAt
        );

        await _outbox.AddMessageAsync(domainEvent, ct);

        // 3. ONE SaveChanges → atomic commit of both rows
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[OrderService] Order {OrderId} created and OutboxMessage queued in one transaction.",
            order.Id);

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default)
        => await _db.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
}
