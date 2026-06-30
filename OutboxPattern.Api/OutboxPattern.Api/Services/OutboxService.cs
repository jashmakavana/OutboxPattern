using Newtonsoft.Json;
using OutboxPattern.Api.Data;
using OutboxPattern.Api.Models;

namespace OutboxPattern.Api.Services;

// ─── Abstraction ────────────────────────────────────────────────────────────

public interface IOutboxService
{
    /// <summary>
    /// Adds an event to the outbox. Must be called WITHIN an active
    /// DbContext transaction so it commits atomically with your business data.
    /// </summary>
    Task AddMessageAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : class;
}

// ─── Implementation ─────────────────────────────────────────────────────────

public class OutboxService : IOutboxService
{
    private readonly AppDbContext _db;

    public OutboxService(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddMessageAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : class
    {
        var message = new OutboxMessage
        {
            Id         = Guid.NewGuid(),
            EventType  = typeof(TEvent).Name,
            Payload    = JsonConvert.SerializeObject(domainEvent),
            CreatedAt  = DateTime.UtcNow,
            ProcessedAt = null   // not yet processed
        };

        await _db.OutboxMessages.AddAsync(message, ct);
        // NOTE: We do NOT call SaveChanges here.
        // The caller (OrderService) calls SaveChanges once, committing
        // the Order row AND the OutboxMessage row in a single transaction.
    }
}
