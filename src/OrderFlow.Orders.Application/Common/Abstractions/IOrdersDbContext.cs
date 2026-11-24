using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Common.Abstractions;

public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
