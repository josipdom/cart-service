using Cart.Domain.Exceptions;
using static Cart.Domain.Tests.TestData;

namespace Cart.Domain.Tests;

public class ShoppingCartTests
{
    [Fact]
    public void Create_ReturnsEmptyCartForUser()
    {
        var cart = ShoppingCart.Create(UserId, Now);

        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Equal(UserId, cart.UserId);
        Assert.Equal(Now, cart.CreatedAt);
        Assert.Equal(Now, cart.UpdatedAt);
        Assert.Equal(0, cart.Version);
        Assert.Empty(cart.Items);
        Assert.Empty(cart.DomainEvents);
        Assert.Equal(0m, cart.Total);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankUserId_Throws(string userId)
    {
        Assert.Throws<DomainException>(() => ShoppingCart.Create(userId, Now));
    }

    [Fact]
    public void Total_SumsLineTotalsOfAllItems()
    {
        var cart = NewCart();

        cart.AddItem("SKU-1001", "Keyboard", 49.99m, 2, Now);
        cart.AddItem("SKU-1002", "Mouse", 19.50m, 3, Now);

        Assert.Equal(158.48m, cart.Total);
    }

    [Fact]
    public void Version_IncrementsOnEveryChange()
    {
        var cart = NewCart();
        var later = Now.AddMinutes(5);

        cart.AddItem("SKU-1001", "Keyboard", 49.99m, 1, Now);
        cart.AddItem("SKU-1002", "Mouse", 19.50m, 1, Now);
        cart.ChangeQuantity("SKU-1001", 3, later);

        Assert.Equal(3, cart.Version);
        Assert.Equal(later, cart.UpdatedAt);
    }

    [Fact]
    public void ClearDomainEvents_RemovesRecordedEvents()
    {
        var cart = NewCart();
        cart.AddItem(ProductId, ProductName, UnitPrice, 1, Now);

        cart.ClearDomainEvents();

        Assert.Empty(cart.DomainEvents);
    }
}
