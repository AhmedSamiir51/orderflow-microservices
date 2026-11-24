using FluentValidation.TestHelper;
using OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;

namespace OrderFlow.Orders.Application.Tests.Features;

public sealed class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    public void Fails_for_invalid_email()
    {
        var command = new CreateOrderCommand("not-an-email", [new CreateOrderLine("SKU-1", 1, 1m)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void Fails_for_empty_lines()
    {
        var command = new CreateOrderCommand("customer@example.com", []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public void Fails_for_non_positive_quantity()
    {
        var command = new CreateOrderCommand("customer@example.com", [new CreateOrderLine("SKU-1", 0, 1m)]);

        var result = _validator.TestValidate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Passes_for_a_valid_command()
    {
        var command = new CreateOrderCommand("customer@example.com", [new CreateOrderLine("SKU-1", 1, 9.99m)]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
