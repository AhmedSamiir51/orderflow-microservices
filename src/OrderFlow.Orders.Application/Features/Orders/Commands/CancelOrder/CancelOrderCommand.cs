using MediatR;

namespace OrderFlow.Orders.Application.Features.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest;
