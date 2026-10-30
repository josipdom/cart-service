namespace Cart.Domain.Tests;

internal static class TestData
{
    public const string UserId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
    public const string ProductId = "SKU-1001";
    public const string ProductName = "Mechanical keyboard";
    public const decimal UnitPrice = 49.99m;

    public static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    public static ShoppingCart NewCart() => ShoppingCart.Create(UserId, Now);

    public static ShoppingCart CartWithItem(int quantity = 1)
    {
        var cart = NewCart();
        cart.AddItem(ProductId, ProductName, UnitPrice, quantity, Now);
        cart.ClearDomainEvents();
        return cart;
    }
}
