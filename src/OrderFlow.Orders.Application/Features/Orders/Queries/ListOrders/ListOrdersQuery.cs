using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application.Common.Abstractions;

namespace OrderFlow.Orders.Application.Features.Orders.Queries.ListOrders;

public sealed record ListOrdersQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<PagedOrders>;

public sealed record PagedOrders(IReadOnlyList<OrderDto> Items, int Page, int PageSize, int TotalCount);

public sealed class ListOrdersHandler(IOrdersDbContext db) : IRequestHandler<ListOrdersQuery, PagedOrders>
{
    public async Task<PagedOrders> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Orders.AsNoTracking().Include(o => o.Lines).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<Domain.Orders.OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedOrders(items.Select(OrderDto.FromDomain).ToList(), page, pageSize, total);
    }
}
