using MassTransit;
using OrderFlow.Contracts.Orders;

namespace OrderFlow.Notifications.Worker.Consumers;

/// <summary>Simulates the confirmation email a customer receives once their order is confirmed.</summary>
public sealed class OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Sending order confirmation email to {Email} for order {OrderId}",
            message.CustomerEmail, message.OrderId);

        // A real implementation would call an email/SMS/push provider here.
        return Task.CompletedTask;
    }
}
