using Cart.Domain.Events;
using Cart.Domain.Exceptions;
using static Cart.Domain.Tests.TestData;

namespace Cart.Domain.Tests;

public class AddItemTests
{
    [Fact]
    public void AddItem_NewProduct_AddsItemWithCatalogDetails()
    {
        var cart = NewCart();

        cart.AddItem(ProductId, ProductName, UnitPrice, 2, Now);

        var item = Assert.Single(cart.Items);
        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal(ProductName, item.ProductName);
        Assert.Equal(UnitPrice, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(99.98m, item.LineTotal);
    }

    [Fact]
    public void AddItem_SameProductTwice_IncreasesQuantity()
    {
        var cart = NewCart();

        cart.AddItem(ProductId, ProductName, UnitPrice, 2, Now);
        cart.AddItem(ProductId, ProductName, UnitPrice, 3, Now);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void AddItem_SameProductAgain_RefreshesNameAndPrice()
    {
        var cart = CartWithItem();

        cart.AddItem(ProductId, "Mechanical keyboard v2", 44.99m, 1, Now);

        var item = Assert.Single(cart.Items);
        Assert.Equal("Mechanical keyboard v2", item.ProductName);
        Assert.Equal(44.99m, item.UnitPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ShoppingCart.MaxQuantityPerItem + 1)]
    public void AddItem_QuantityOutOfRange_Throws(int quantity)
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.AddItem(ProductId, ProductName, UnitPrice, quantity, Now));
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_CombinedQuantityReachesMax_IsAllowed()
    {
        var cart = CartWithItem(quantity: 50);

        cart.AddItem(ProductId, ProductName, UnitPrice, 49, Now);

        Assert.Equal(ShoppingCart.MaxQuantityPerItem, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void AddItem_CombinedQuantityAboveMax_ThrowsAndKeepsQuantity()
    {
        var cart = CartWithItem(quantity: 98);

        Assert.Throws<DomainException>(() => cart.AddItem(ProductId, ProductName, UnitPrice, 2, Now));
        Assert.Equal(98, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void AddItem_MoreThanMaxDistinctProducts_Throws()
    {
        var cart = NewCart();
        for (var i = 1; i <= ShoppingCart.MaxDistinctItems; i++)
            cart.AddItem($"SKU-{i}", $"Product {i}", 1m, 1, Now);

        Assert.Throws<DomainException>(() => cart.AddItem("SKU-NEW", "One too many", 1m, 1, Now));
        Assert.Equal(ShoppingCart.MaxDistinctItems, cart.Items.Count);
    }

    [Fact]
    public void AddItem_ExistingProductWhenCartIsFull_IsAllowed()
    {
        var cart = NewCart();
        for (var i = 1; i <= ShoppingCart.MaxDistinctItems; i++)
            cart.AddItem($"SKU-{i}", $"Product {i}", 1m, 1, Now);

        cart.AddItem("SKU-1", "Product 1", 1m, 1, Now);

        Assert.Equal(2, cart.Items.Single(i => i.ProductId == "SKU-1").Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddItem_NonPositivePrice_Throws(decimal unitPrice)
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.AddItem(ProductId, ProductName, unitPrice, 1, Now));
        Assert.Empty(cart.Items);
    }

    [Theory]
    [InlineData("", ProductName)]
    [InlineData(ProductId, " ")]
    public void AddItem_BlankProductIdOrName_Throws(string productId, string productName)
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.AddItem(productId, productName, UnitPrice, 1, Now));
    }

    [Fact]
    public void AddItem_RaisesCartItemAdded()
    {
        var cart = CartWithItem(quantity: 2);

        cart.AddItem(ProductId, ProductName, UnitPrice, 3, Now);

        var added = Assert.IsType<CartItemAdded>(Assert.Single(cart.DomainEvents));
        Assert.Equal(cart.Id, added.CartId);
        Assert.Equal(UserId, added.UserId);
        Assert.Equal(ProductId, added.ProductId);
        Assert.Equal(3, added.QuantityAdded);
        Assert.Equal(5, added.TotalQuantity);
        Assert.Equal(UnitPrice, added.UnitPrice);
    }
}
