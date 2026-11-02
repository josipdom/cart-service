using Cart.Domain;
using Cart.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public sealed class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);

    private void AddDomainEventsToOutbox()
    {
        var carts = ChangeTracker.Entries<ShoppingCart>()
            .Select(entry => entry.Entity)
            .Where(cart => cart.DomainEvents.Count > 0)
            .ToList();

        foreach (var cart in carts)
        {
            OutboxMessages.AddRange(cart.DomainEvents.Select(OutboxMessage.From));
            cart.ClearDomainEvents();
        }
    }
}
