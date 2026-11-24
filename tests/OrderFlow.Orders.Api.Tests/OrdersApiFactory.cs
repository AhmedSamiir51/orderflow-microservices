using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Orders.Infrastructure.Persistence;

namespace OrderFlow.Orders.Api.Tests;

/// <summary>
/// Boots the real API with SQLite and the in-memory transport, so tests exercise the full
/// stack (controllers, MediatR, EF Core, MassTransit consumers, outbox) without Docker.
/// </summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = $"orders-{Guid.NewGuid():N}";
    private SqliteConnection? _keepAlive;

    public OrdersApiFactory()
    {
        // Environment variables override appsettings.json and are visible while Program.cs
        // registers services, unlike WebApplicationFactory.ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("Database__MigrateOnStartup", "false");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_database};Mode=Memory;Cache=Shared");
        Environment.SetEnvironmentVariable("Transport__Provider", "InMemory");
    }


    public async Task InitializeAsync()
    {
        _keepAlive = new SqliteConnection($"Data Source={_database};Mode=Memory;Cache=Shared");
        await _keepAlive.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive?.Dispose();
        }
    }
}

/// <summary>Collection fixture: one factory (and one database) per test class run.</summary>
[CollectionDefinition(Name)]
public sealed class OrdersApiCollection : ICollectionFixture<OrdersApiFactory>
{
    public const string Name = "orders-api";
}
