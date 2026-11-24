using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Application.Common.Exceptions;

namespace OrderFlow.Inventory.Application.Features.Stock.Commands.RestockStockItem;

public sealed record RestockStockItemCommand(string Sku, int Quantity) : IRequest<StockItemDto>;

public sealed class RestockStockItemValidator : AbstractValidator<RestockStockItemCommand>
{
    public RestockStockItemValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class RestockStockItemHandler(IInventoryDbContext db) : IRequestHandler<RestockStockItemCommand, StockItemDto>
{
    public async Task<StockItemDto> Handle(RestockStockItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(i => i.Sku == request.Sku, cancellationToken)
            ?? throw new NotFoundException("StockItem", request.Sku);

        item.Restock(request.Quantity);
        await db.SaveChangesAsync(cancellationToken);
        return StockItemDto.FromDomain(item);
    }
}
