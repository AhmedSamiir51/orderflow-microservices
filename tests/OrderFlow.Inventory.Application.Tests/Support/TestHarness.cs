using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Infrastructure.Persistence;

namespace OrderFlow.Inventory.Application.Tests.Support;

/// <summary>An EF Core context over a shared in-memory SQLite database, created once per test.</summary>
public sealed class TestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDb(SqliteConnection connection, InventoryDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public InventoryDbContext Context { get; }

    public static async Task<TestDb> CreateAsync()
    {
        var connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new InventoryDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDb(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>Captures published integration events instead of talking to a broker.</summary>
public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<object> Published { get; } = [];

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<T> OfType<T>() => Published.OfType<T>().ToList();
}

/// <summary>A no-op logger for handlers under test.</summary>
public sealed class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}
