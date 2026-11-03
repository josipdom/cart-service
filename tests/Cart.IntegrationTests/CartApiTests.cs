using System.Net;
using System.Net.Http.Json;
using Cart.Application.Carts;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CartApiTests(CartApiFactory factory)
{
    private const string CartUrl = "/api/user/cart";
    private const string ItemsUrl = "/api/user/cart/items";

    [Fact]
    public async Task GetCart_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync(CartUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_ThenGetCart_ReturnsItemWithCatalogPrice()
    {
        var client = await factory.CreateUserClientAsync(Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync(ItemsUrl, new AddItemRequest("SKU-1001", 2));
        response.EnsureSuccessStatusCode();
        var cart = await client.GetFromJsonAsync<CartDto>(CartUrl);

        var item = Assert.Single(cart!.Items);
        Assert.Equal(49.99m, item.UnitPrice);
        Assert.Equal(99.98m, cart.Total);
    }

    [Fact]
    public async Task AddItem_PublishesEventThroughOutbox()
    {
        var client = await factory.CreateUserClientAsync(Guid.NewGuid().ToString());
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        await client.PostAsJsonAsync(ItemsUrl, new AddItemRequest("SKU-1003", 1));

        Assert.True(await WaitUntilPublishedAsync(correlationId), "The outbox message was not published in time.");
    }

    private async Task<bool> WaitUntilPublishedAsync(string correlationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

            if (await dbContext.OutboxMessages.AnyAsync(m => m.CorrelationId == correlationId && m.ProcessedAt != null))
                return true;

            await Task.Delay(250);
        }

        return false;
    }
}
