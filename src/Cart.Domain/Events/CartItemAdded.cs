namespace Cart.Domain.Events;

public sealed record CartItemAdded(
    Guid CartId,
    string UserId,
    string ProductId,
    int QuantityAdded,
    int TotalQuantity,
    decimal UnitPrice,
    DateTimeOffset OccurredAt) : IDomainEvent;
