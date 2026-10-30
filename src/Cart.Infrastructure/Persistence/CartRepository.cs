using Cart.Application.Abstractions;
using Cart.Application.Exceptions;
using Cart.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cart.Infrastructure.Persistence;

internal sealed class CartRepository(CartDbContext dbContext) : ICartRepository
{
    public Task<ShoppingCart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Carts.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public void Add(ShoppingCart cart) => dbContext.Carts.Add(cart);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two first requests for the same user both tried to create a cart.
            throw new ConcurrencyConflictException(exception);
        }
    }
}
