namespace OrderFlow.Inventory.Domain;

/// <summary>Raised when an aggregate invariant is violated. Mapped to HTTP 409 at the edge.</summary>
public sealed class DomainException(string message) : Exception(message);
