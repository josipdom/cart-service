namespace Cart.Domain.Events;

public sealed record CartCleared(
    Guid CartId,
    string UserId,
    DateTimeOffset OccurredAt) : IDomainEvent;
