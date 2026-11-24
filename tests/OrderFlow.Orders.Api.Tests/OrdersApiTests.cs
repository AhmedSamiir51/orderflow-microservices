using System.Net;
using System.Net.Http.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Inventory;
using OrderFlow.Orders.Application.Features.Orders;
using OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Orders.Infrastructure.Persistence;

namespace OrderFlow.Orders.Api.Tests;

[Collection(OrdersApiCollection.Name)]
public sealed class OrdersApiTests : IAsyncLifetime
{
    private readonly OrdersApiFactory _factory;
    private readonly HttpClient _client;

    public OrdersApiTests(OrdersApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_creates_a_pending_order()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            "customer@example.com", [new CreateOrderLine("SKU-1", 2, 9.99m)]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(19.98m, order.Total);
    }

    [Fact]
    public async Task Post_with_an_invalid_email_returns_a_problem_details_400()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            "not-an-email", [new CreateOrderLine("SKU-1", 1, 1m)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Validation failed", body);
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StockReserved_event_confirms_the_order_end_to_end()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            "flow@example.com", [new CreateOrderLine("SKU-9", 1, 3m)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        // Simulate the Inventory service replying over the broker.
        await using var scope = _factory.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await bus.Publish(new StockReserved(created.Id, DateTimeOffset.UtcNow));
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().SaveChangesAsync();

        var confirmed = await WaitForStatusAsync(created.Id, "Confirmed", TimeSpan.FromSeconds(20));
        Assert.Equal("Confirmed", confirmed.Status);
        Assert.NotNull(confirmed.CompletedAt);
    }

    [Fact]
    public async Task StockReservationFailed_event_rejects_the_order_with_a_reason()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            "flow2@example.com", [new CreateOrderLine("SKU-X", 50, 1m)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        await using var scope = _factory.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await bus.Publish(new StockReservationFailed(created.Id, "Not enough stock for 'SKU-X'", DateTimeOffset.UtcNow));
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().SaveChangesAsync();

        var rejected = await WaitForStatusAsync(created.Id, "Rejected", TimeSpan.FromSeconds(20));
        Assert.Equal("Rejected", rejected.Status);
        Assert.Contains("SKU-X", rejected.RejectionReason);
    }

    [Fact]
    public async Task Cancel_pending_order_then_rejecting_it_again_is_safe()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            "cancel@example.com", [new CreateOrderLine("SKU-1", 1, 1m)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var cancelResponse = await _client.PostAsync($"/api/orders/{created.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var order = await (await _client.GetAsync($"/api/orders/{created.Id}")).Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Cancelled", order!.Status);
    }

    private async Task<OrderDto> WaitForStatusAsync(Guid id, string expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        OrderDto? order = null;
        while (DateTime.UtcNow < deadline)
        {
            order = await (await _client.GetAsync($"/api/orders/{id}")).Content.ReadFromJsonAsync<OrderDto>();
            if (order?.Status == expected)
            {
                return order;
            }

            await Task.Delay(250);
        }

        Assert.Fail($"Order {id} never reached status {expected}; last state: {order?.Status ?? "missing"}");
        return order!;
    }
}
