using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Orders.Application.Features.Orders;
using OrderFlow.Orders.Application.Features.Orders.Commands.CancelOrder;
using OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Orders.Application.Features.Orders.Queries.GetOrderById;
using OrderFlow.Orders.Application.Features.Orders.Queries.ListOrders;

namespace OrderFlow.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender mediator) : ControllerBase
{
    /// <summary>Places a new order. Stock is reserved asynchronously by the Inventory service.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetOrderByIdQuery(id), cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedOrders), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        return Ok(await mediator.Send(new ListOrdersQuery(page, pageSize, status), cancellationToken));
    }

    /// <summary>Cancels an order while it is still pending.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
