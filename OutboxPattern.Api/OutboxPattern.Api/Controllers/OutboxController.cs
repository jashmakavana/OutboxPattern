using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPattern.Api.Data;
using OutboxPattern.Api.Models;
using OutboxPattern.Api.Services;

namespace OutboxPattern.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutboxController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageBroker _broker;

    public OutboxController(AppDbContext db, IMessageBroker broker)
    {
        _db     = db;
        _broker = broker;
    }

    /// <summary>Returns ALL outbox messages (processed + pending).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OutboxMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMessages(CancellationToken ct)
    {
        var messages = await _db.OutboxMessages
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
        return Ok(messages);
    }

    /// <summary>Returns only unprocessed (pending) outbox messages.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<OutboxMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMessages(CancellationToken ct)
    {
        var messages = await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return Ok(messages);
    }

    /// <summary>Returns messages that were published to the (fake) broker.</summary>
    [HttpGet("published")]
    public IActionResult GetPublishedMessages()
        => Ok(_broker.GetPublishedMessages());

    /// <summary>Returns a summary of outbox statistics.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var total     = await _db.OutboxMessages.CountAsync(ct);
        var processed = await _db.OutboxMessages.CountAsync(m => m.ProcessedAt != null, ct);
        var pending   = await _db.OutboxMessages.CountAsync(m => m.ProcessedAt == null, ct);
        var failed    = await _db.OutboxMessages.CountAsync(m => m.Error != null && m.ProcessedAt == null, ct);
        var published = _broker.GetPublishedMessages().Count;

        return Ok(new
        {
            TotalOutboxMessages  = total,
            Processed            = processed,
            Pending              = pending,
            Failed               = failed,
            PublishedToBroker    = published
        });
    }
}
