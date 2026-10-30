using System.Diagnostics.CodeAnalysis;
using Cart.Domain.Exceptions;

namespace Cart.Domain;

public sealed class CartItem
{
    internal CartItem(string productId, string productName, decimal unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new DomainException("Product id is required.");

        ProductId = productId;
        Quantity = quantity;
        UpdateProductDetails(productName, unitPrice);
    }

    public string ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    [MemberNotNull(nameof(ProductName))]
    internal void UpdateProductDetails(string productName, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        ProductName = productName;
        UnitPrice = unitPrice;
    }

    internal void SetQuantity(int quantity) => Quantity = quantity;
}
