using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Inventory.Application.Features.Stock;
using OrderFlow.Inventory.Application.Features.Stock.Commands.CreateStockItem;
using OrderFlow.Inventory.Application.Features.Stock.Commands.RestockStockItem;
using OrderFlow.Inventory.Application.Features.Stock.Queries.ListStock;

namespace OrderFlow.Inventory.Api.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ListStockQuery(), cancellationToken));
    }

    /// <summary>Creates a stock item. Seeded data for trying the order flow.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStockItemCommand command, CancellationToken cancellationToken)
    {
        var item = await mediator.Send(command, cancellationToken);
        return Created($"/api/stock/{item.Sku}", item);
    }

    [HttpPost("{sku}/restock")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restock(string sku, [FromBody] RestockRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new RestockStockItemCommand(sku, request.Quantity), cancellationToken));
    }
}

public sealed record RestockRequest(int Quantity);
