using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Infrastructure.Messaging;
using OrderFlow.Inventory.Infrastructure.Messaging.Consumers;
using OrderFlow.Inventory.Infrastructure.Persistence;

namespace OrderFlow.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=orderflow_inventory;Username=postgres;Password=postgres";

        services.AddDbContext<InventoryDbContext>(options =>
        {
            if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });
        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddConsumer<OrderCreatedConsumer>();

            bus.AddEntityFrameworkOutbox<InventoryDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);

                if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    outbox.UseSqlite();
                }
                else
                {
                    outbox.UsePostgres();
                }

                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, name, endpoint) =>
            {
                endpoint.UseEntityFrameworkOutbox<InventoryDbContext>(context);
                endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(300)));
            });

            var transport = configuration["Transport:Provider"] ?? "RabbitMq";
            if (transport.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
            else
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    var rabbit = configuration.GetSection("RabbitMq");
                    cfg.Host(rabbit["Host"] ?? "localhost", "/", host =>
                    {
                        host.Username(rabbit["Username"] ?? "guest");
                        host.Password(rabbit["Password"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
