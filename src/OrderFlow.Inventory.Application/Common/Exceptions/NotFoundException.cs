namespace OrderFlow.Inventory.Application.Common.Exceptions;

/// <summary>Raised when an aggregate cannot be found. Mapped to HTTP 404 at the edge.</summary>
public sealed class NotFoundException(string resource, object key) : Exception($"{resource} '{key}' was not found.");
