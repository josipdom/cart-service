using Cart.Domain;

namespace Cart.Application.Abstractions;

public interface ICartRepository
{
    Task<ShoppingCart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(ShoppingCart cart);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
