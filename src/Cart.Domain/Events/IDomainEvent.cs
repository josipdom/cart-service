namespace Cart.Domain.Events;

public interface IDomainEvent
{
    Guid CartId { get; }
    string UserId { get; }
    DateTimeOffset OccurredAt { get; }
}
