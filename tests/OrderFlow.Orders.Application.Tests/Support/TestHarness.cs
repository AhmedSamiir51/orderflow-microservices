using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Orders;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Infrastructure.Persistence;

namespace OrderFlow.Orders.Application.Tests.Support;

/// <summary>An EF Core context over a shared in-memory SQLite database, created once per test.</summary>
public sealed class TestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDb(SqliteConnection connection, OrdersDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public OrdersDbContext Context { get; }

    public static async Task<TestDb> CreateAsync()
    {
        var connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new OrdersDbContext(options);
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

/// <summary>A controllable clock for deterministic timestamps.</summary>
public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
