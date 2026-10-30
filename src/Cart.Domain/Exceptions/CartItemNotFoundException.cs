namespace Cart.Domain.Exceptions;

public sealed class CartItemNotFoundException(string productId)
    : DomainException($"Product '{productId}' is not in the cart.")
{
    public string ProductId { get; } = productId;
}
