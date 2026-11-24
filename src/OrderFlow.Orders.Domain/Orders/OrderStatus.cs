namespace OrderFlow.Orders.Domain.Orders;

public enum OrderStatus
{
    /// <summary>Accepted, waiting for the Inventory service to reserve stock.</summary>
    Pending = 0,

    /// <summary>Stock reserved; the order is confirmed.</summary>
    Confirmed = 1,

    /// <summary>Stock could not be reserved; the order is rejected.</summary>
    Rejected = 2,

    /// <summary>Cancelled by the customer while still pending.</summary>
    Cancelled = 3
}
