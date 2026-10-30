using Cart.Domain;

namespace Cart.Application.Carts;

public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal Total, string Currency)
{
    public const string DefaultCurrency = "EUR";

    public static CartDto Empty { get; } = new([], 0m, DefaultCurrency);

    public static CartDto From(ShoppingCart cart) => new(
        cart.Items.Select(CartItemDto.From).ToList(),
        cart.Total,
        DefaultCurrency);
}

public sealed record CartItemDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public static CartItemDto From(CartItem item) =>
        new(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal);
}
