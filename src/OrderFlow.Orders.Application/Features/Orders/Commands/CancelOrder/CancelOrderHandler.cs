using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Application.Common.Exceptions;

namespace OrderFlow.Orders.Application.Features.Orders.Commands.CancelOrder;

public sealed class CancelOrderHandler(IOrdersDbContext db, TimeProvider time) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        order.Cancel(time);
        await db.SaveChangesAsync(cancellationToken);
    }
}
