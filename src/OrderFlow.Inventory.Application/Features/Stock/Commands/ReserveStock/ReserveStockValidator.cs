using FluentValidation;

namespace OrderFlow.Inventory.Application.Features.Stock.Commands.ReserveStock;

public sealed class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Sku).NotEmpty().MaximumLength(64);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
