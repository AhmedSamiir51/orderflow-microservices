using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Application.Features.Stock.Commands.CreateStockItem;

public sealed record CreateStockItemCommand(string Sku, string Name, int InitialQuantity) : IRequest<StockItemDto>;

public sealed class CreateStockItemValidator : AbstractValidator<CreateStockItemCommand>
{
    public CreateStockItemValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateStockItemHandler(IInventoryDbContext db) : IRequestHandler<CreateStockItemCommand, StockItemDto>
{
    public async Task<StockItemDto> Handle(CreateStockItemCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.StockItems.AnyAsync(i => i.Sku == request.Sku.Trim(), cancellationToken);
        if (exists)
        {
            throw new DomainException($"A stock item with SKU '{request.Sku}' already exists.");
        }

        var item = StockItem.Create(request.Sku, request.Name, request.InitialQuantity);
        db.StockItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return StockItemDto.FromDomain(item);
    }
}
