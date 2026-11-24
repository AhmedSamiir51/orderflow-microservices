using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Application.Common.Exceptions;

namespace OrderFlow.Orders.Application.Features.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public sealed class GetOrderByIdHandler(IOrdersDbContext db) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        return OrderDto.FromDomain(order);
    }
}
