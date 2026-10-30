namespace Cart.Domain.Events;

public sealed record CartItemQuantityChanged(
    Guid CartId,
    string UserId,
    string ProductId,
    int OldQuantity,
    int NewQuantity,
    DateTimeOffset OccurredAt) : IDomainEvent;
