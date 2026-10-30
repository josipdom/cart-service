namespace Cart.Application.Abstractions;

public interface IProductCatalog
{
    Task<ProductInfo?> GetProductAsync(string productId, CancellationToken cancellationToken);
}

public sealed record ProductInfo(string ProductId, string Name, decimal UnitPrice);
