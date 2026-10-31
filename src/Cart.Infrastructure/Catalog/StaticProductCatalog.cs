using Cart.Application.Abstractions;

namespace Cart.Infrastructure.Catalog;

internal sealed class StaticProductCatalog : IProductCatalog
{
    private static readonly Dictionary<string, ProductInfo> Products = new()
    {
        ["SKU-1001"] = new("SKU-1001", "Mechanical keyboard", 49.99m),
        ["SKU-1002"] = new("SKU-1002", "Wireless mouse", 19.50m),
        ["SKU-1003"] = new("SKU-1003", "USB-C hub", 34.90m),
        ["SKU-1004"] = new("SKU-1004", "27-inch monitor", 229.00m),
        ["SKU-1005"] = new("SKU-1005", "Laptop stand", 27.45m),
    };

    public Task<ProductInfo?> GetProductAsync(string productId, CancellationToken cancellationToken) =>
        Task.FromResult(Products.GetValueOrDefault(productId));
}
