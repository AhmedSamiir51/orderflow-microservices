using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Inventory.Infrastructure.Persistence;

namespace OrderFlow.Inventory.Api.Tests;

/// <summary>Boots the real Inventory API with SQLite and the in-memory transport.</summary>
public sealed class InventoryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = $"inventory-{Guid.NewGuid():N}";
    private SqliteConnection? _keepAlive;

    public InventoryApiFactory()
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
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
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
