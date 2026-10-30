using Cart.Domain.Events;
using Cart.Domain.Exceptions;
using static Cart.Domain.Tests.TestData;

namespace Cart.Domain.Tests;

public class ChangeQuantityTests
{
    [Fact]
    public void ChangeQuantity_ExistingItem_SetsAbsoluteQuantity()
    {
        var cart = CartWithItem(quantity: 2);

        cart.ChangeQuantity(ProductId, 7, Now);

        Assert.Equal(7, Assert.Single(cart.Items).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ShoppingCart.MaxQuantityPerItem + 1)]
    public void ChangeQuantity_QuantityOutOfRange_Throws(int quantity)
    {
        var cart = CartWithItem(quantity: 2);

        Assert.Throws<DomainException>(() => cart.ChangeQuantity(ProductId, quantity, Now));
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void ChangeQuantity_ProductNotInCart_ThrowsNotFound()
    {
        var cart = CartWithItem();

        var exception = Assert.Throws<CartItemNotFoundException>(() => cart.ChangeQuantity("SKU-MISSING", 1, Now));
        Assert.Equal("SKU-MISSING", exception.ProductId);
    }

    [Fact]
    public void ChangeQuantity_SameQuantity_DoesNotRecordChange()
    {
        var cart = CartWithItem(quantity: 2);
        var version = cart.Version;

        cart.ChangeQuantity(ProductId, 2, Now.AddMinutes(1));

        Assert.Empty(cart.DomainEvents);
        Assert.Equal(version, cart.Version);
    }

    [Fact]
    public void ChangeQuantity_RaisesCartItemQuantityChanged()
    {
        var cart = CartWithItem(quantity: 2);

        cart.ChangeQuantity(ProductId, 5, Now);

        var changed = Assert.IsType<CartItemQuantityChanged>(Assert.Single(cart.DomainEvents));
        Assert.Equal(ProductId, changed.ProductId);
        Assert.Equal(2, changed.OldQuantity);
        Assert.Equal(5, changed.NewQuantity);
    }
}
