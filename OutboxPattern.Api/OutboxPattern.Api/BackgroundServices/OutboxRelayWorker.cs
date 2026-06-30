using Microsoft.EntityFrameworkCore;
using OutboxPattern.Api.Data;
using OutboxPattern.Api.Services;

namespace OutboxPattern.Api.BackgroundServices;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║              OUTBOX RELAY WORKER                                 ║
/// ║                                                                  ║
/// ║  Runs as a .NET BackgroundService (hosted service).              ║
/// ║  Every N seconds it:                                             ║
/// ║    1. Queries OutboxMessages WHERE ProcessedAt IS NULL           ║
/// ║    2. For each message → calls the message broker                ║
/// ║    3. On success → stamps ProcessedAt = UtcNow                   ║
/// ║    4. On failure → increments RetryCount, stores error           ║
/// ║    5. SaveChanges to persist the updated status                  ║
/// ║                                                                  ║
/// ║  This separation ensures the API thread is never blocked         ║
/// ║  waiting for a broker, and messages survive process restarts.    ║
/// ╚══════════════════════════════════════════════════════════════════╝
/// </summary>
public class OutboxRelayWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayWorker> _logger;

    public OutboxRelayWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxRelayWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxRelay] Worker started. Polling every {Interval}s.", PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(stoppingToken);
            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        // Use a new scope per iteration — DbContext is scoped, worker is singleton
        using var scope  = _scopeFactory.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var broker       = scope.ServiceProvider.GetRequiredService<IMessageBroker>();

        // Fetch up to 20 unprocessed messages, ordered oldest-first
        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            _logger.LogDebug("[OutboxRelay] No pending messages.");
            return;
        }

        _logger.LogInformation("[OutboxRelay] Processing {Count} pending outbox message(s).", pending.Count);

        foreach (var message in pending)
        {
            try
            {
                // Publish to the message broker (RabbitMQ / Service Bus / Kafka etc.)
                await broker.PublishAsync(message.EventType, message.Payload, ct);

                // Mark as successfully processed
                message.ProcessedAt = DateTime.UtcNow;
                message.Error       = null;

                _logger.LogInformation(
                    "[OutboxRelay] ✅ Message {Id} ({EventType}) published successfully.",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogWarning(
                    "[OutboxRelay] ⚠️ Message {Id} failed (attempt {Retry}/{Max}): {Error}",
                    message.Id, message.RetryCount, MaxRetries, ex.Message);
            }

            // Persist the ProcessedAt / RetryCount changes for this message
            await db.SaveChangesAsync(ct);
        }
    }
}
