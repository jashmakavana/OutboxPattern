using Microsoft.AspNetCore.Mvc;
using OutboxPattern.Api.Models;
using OutboxPattern.Api.Services;

namespace OutboxPattern.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Creates an order and atomically enqueues an OrderCreatedEvent
    /// in the outbox — all in a single DB transaction.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _orderService.CreateOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Order>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        var order  = orders.FirstOrDefault(o => o.Id == id);
        return order is null ? NotFound() : Ok(order);
    }
}
