using System.Diagnostics;
using System.Text.Json;
using Cart.Domain.Events;
using Cart.Infrastructure.Outbox;

namespace Cart.IntegrationTests.Outbox;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static readonly CartItemAdded ItemAdded =
        new(Guid.NewGuid(), "f47ac10b-58cc-4372-a567-0e02b2c3d479", "SKU-1001", 2, 3, 49.99m, Now);

    [Fact]
    public void From_DomainEvent_UsesEventNameAndTime()
    {
        var message = OutboxMessage.From(ItemAdded);

        Assert.Equal(nameof(CartItemAdded), message.Type);
        Assert.Equal(Now, message.OccurredAt);
        Assert.Equal(7, message.Id.Version);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void From_DomainEvent_SerializesAllEventProperties()
    {
        var message = OutboxMessage.From(ItemAdded);

        var payload = JsonSerializer.Deserialize<CartItemAdded>(message.Payload, JsonSerializerOptions.Web);

        Assert.Equal(ItemAdded, payload);
    }

    [Fact]
    public void From_DomainEvent_TakesCorrelationIdFromCurrentActivity()
    {
        using var activity = new Activity("request")
            .SetBaggage(OutboxMessage.CorrelationIdBaggage, "order-flow-42")
            .Start();

        var message = OutboxMessage.From(ItemAdded);

        Assert.Equal("order-flow-42", message.CorrelationId);
    }
}
