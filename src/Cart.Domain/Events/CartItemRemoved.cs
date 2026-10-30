namespace Cart.Domain.Events;

public sealed record CartItemRemoved(
    Guid CartId,
    string UserId,
    string ProductId,
    DateTimeOffset OccurredAt) : IDomainEvent;
