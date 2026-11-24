using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Features.Orders;

public sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    Guid Id,
    string CustomerEmail,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? RejectionReason,
    IReadOnlyList<OrderLineDto> Lines)
{
    public static OrderDto FromDomain(Order order) => new(
        order.Id,
        order.CustomerEmail,
        order.Status.ToString(),
        order.Total,
        order.CreatedAt,
        order.CompletedAt,
        order.RejectionReason,
        order.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList());
}
