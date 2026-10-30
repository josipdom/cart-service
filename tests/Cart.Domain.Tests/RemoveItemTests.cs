using Cart.Domain.Events;
using Cart.Domain.Exceptions;
using static Cart.Domain.Tests.TestData;

namespace Cart.Domain.Tests;

public class RemoveItemTests
{
    [Fact]
    public void RemoveItem_ExistingItem_RemovesIt()
    {
        var cart = CartWithItem();

        cart.RemoveItem(ProductId, Now);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveItem_OtherItemsStayInCart()
    {
        var cart = CartWithItem();
        cart.AddItem("SKU-1002", "Mouse", 19.50m, 1, Now);

        cart.RemoveItem(ProductId, Now);

        Assert.Equal("SKU-1002", Assert.Single(cart.Items).ProductId);
    }

    [Fact]
    public void RemoveItem_ProductNotInCart_ThrowsNotFound()
    {
        var cart = CartWithItem();

        var exception = Assert.Throws<CartItemNotFoundException>(() => cart.RemoveItem("SKU-MISSING", Now));
        Assert.Equal("SKU-MISSING", exception.ProductId);
    }

    [Fact]
    public void RemoveItem_RaisesCartItemRemovedAndIncrementsVersion()
    {
        var cart = CartWithItem();
        var version = cart.Version;

        cart.RemoveItem(ProductId, Now);

        var removed = Assert.IsType<CartItemRemoved>(Assert.Single(cart.DomainEvents));
        Assert.Equal(ProductId, removed.ProductId);
        Assert.Equal(version + 1, cart.Version);
    }
}
