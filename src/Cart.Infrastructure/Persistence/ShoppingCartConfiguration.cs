using Cart.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Persistence;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.UserId).HasMaxLength(255);
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.Ignore(c => c.Total);
        builder.Ignore(c => c.DomainEvents);

        builder.OwnsMany(c => c.Items, item =>
        {
            item.ToTable("CartItems");
            item.WithOwner().HasForeignKey("CartId");
            item.HasKey("CartId", nameof(CartItem.ProductId));

            item.Property(i => i.ProductId).HasMaxLength(64);
            item.Property(i => i.ProductName).HasMaxLength(200);
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
            item.Ignore(i => i.LineTotal);
        });
    }
}
