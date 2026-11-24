using MediatR;

namespace OrderFlow.Inventory.Application.Features.Stock.Commands.ReserveStock;

public sealed record ReserveStockLine(string Sku, int Quantity);

/// <summary>Raised by the OrderCreated consumer. The handler publishes the outcome itself.</summary>
public sealed record ReserveStockCommand(Guid OrderId, IReadOnlyList<ReserveStockLine> Lines) : IRequest;
