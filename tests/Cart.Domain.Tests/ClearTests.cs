using Cart.Domain.Events;
using static Cart.Domain.Tests.TestData;

namespace Cart.Domain.Tests;

public class ClearTests
{
    [Fact]
    public void Clear_CartWithItems_RemovesAllItems()
    {
        var cart = CartWithItem();
        cart.AddItem("SKU-1002", "Mouse", 19.50m, 1, Now);

        cart.Clear(Now);

        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public void Clear_RaisesCartCleared()
    {
        var cart = CartWithItem();

        cart.Clear(Now);

        var cleared = Assert.IsType<CartCleared>(Assert.Single(cart.DomainEvents));
        Assert.Equal(cart.Id, cleared.CartId);
    }

    [Fact]
    public void Clear_EmptyCart_DoesNotRecordChange()
    {
        var cart = NewCart();

        cart.Clear(Now);

        Assert.Empty(cart.DomainEvents);
        Assert.Equal(0, cart.Version);
    }
}
