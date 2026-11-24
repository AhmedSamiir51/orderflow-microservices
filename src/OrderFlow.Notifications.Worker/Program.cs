using MassTransit;
using OrderFlow.Notifications.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();

    bus.AddConsumer<OrderConfirmedConsumer>();
    bus.AddConsumer<OrderRejectedConsumer>();

    bus.AddConfigureEndpointsCallback((context, name, endpoint) =>
    {
        endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(300)));
    });

    var transport = builder.Configuration["Transport:Provider"] ?? "RabbitMq";
    if (transport.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
    }
    else
    {
        bus.UsingRabbitMq((context, cfg) =>
        {
            var rabbit = builder.Configuration.GetSection("RabbitMq");
            cfg.Host(rabbit["Host"] ?? "localhost", "/", host =>
            {
                host.Username(rabbit["Username"] ?? "guest");
                host.Password(rabbit["Password"] ?? "guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    }
});

var host = builder.Build();
host.Run();
