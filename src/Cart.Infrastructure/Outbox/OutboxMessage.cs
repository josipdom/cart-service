using System.Diagnostics;
using System.Text.Json;
using Cart.Domain.Events;

namespace Cart.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }

    public static OutboxMessage From(IDomainEvent domainEvent) => new()
    {
        Id = Guid.CreateVersion7(domainEvent.OccurredAt),
        Type = domainEvent.GetType().Name,
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
        OccurredAt = domainEvent.OccurredAt,
        CorrelationId = Activity.Current?.TraceId.ToString()
    };
}
