using MassTransit;
using OrderFlow.Contracts.Orders;

namespace OrderFlow.Notifications.Worker.Consumers;

/// <summary>Simulates the notification a customer receives when their order could not be fulfilled.</summary>
public sealed class OrderRejectedConsumer(ILogger<OrderRejectedConsumer> logger) : IConsumer<OrderRejected>
{
    public Task Consume(ConsumeContext<OrderRejected> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Sending rejection email to {Email} for order {OrderId}: {Reason}",
            message.CustomerEmail, message.OrderId, message.Reason);

        return Task.CompletedTask;
    }
}
