using Cart.Application.Carts;

namespace Cart.Application.Abstractions;

public interface ICartCache
{
    Task<CartDto?> GetAsync(string userId, CancellationToken cancellationToken);
    Task SetAsync(string userId, CartDto cart, CancellationToken cancellationToken);
    Task RemoveAsync(string userId, CancellationToken cancellationToken);
}
