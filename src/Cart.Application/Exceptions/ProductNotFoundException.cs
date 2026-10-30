namespace Cart.Application.Exceptions;

public sealed class ProductNotFoundException(string productId)
    : Exception($"Product '{productId}' was not found.")
{
    public string ProductId { get; } = productId;
}
