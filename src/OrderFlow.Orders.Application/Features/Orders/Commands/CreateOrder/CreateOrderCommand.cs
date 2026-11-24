using MediatR;
using OrderFlow.Orders.Application.Features.Orders;

namespace OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(string CustomerEmail, IReadOnlyList<CreateOrderLine> Lines) : IRequest<OrderDto>;

public sealed record CreateOrderLine(string Sku, int Quantity, decimal UnitPrice);
