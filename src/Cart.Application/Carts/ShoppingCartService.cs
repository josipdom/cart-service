using Cart.Application.Abstractions;
using Cart.Application.Exceptions;
using Cart.Domain;
using Cart.Domain.Exceptions;
using FluentValidation;

namespace Cart.Application.Carts;

public sealed class ShoppingCartService(
    ICartRepository repository,
    ICartCache cache,
    IProductCatalog catalog,
    IValidator<AddItemRequest> addItemValidator,
    IValidator<ChangeQuantityRequest> changeQuantityValidator,
    TimeProvider timeProvider)
{
    public async Task<CartDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(userId, cancellationToken);
        if (cached is not null)
            return cached;

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        var dto = cart is null ? CartDto.Empty : CartDto.From(cart);

        await cache.SetAsync(userId, dto, cancellationToken);
        return dto;
    }

    public async Task<CartDto> AddItemAsync(string userId, AddItemRequest request, CancellationToken cancellationToken)
    {
        await addItemValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = await catalog.GetProductAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(request.ProductId);

        var now = timeProvider.GetUtcNow();
        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = ShoppingCart.Create(userId, now);
            repository.Add(cart);
        }

        cart.AddItem(product.ProductId, product.Name, product.UnitPrice, request.Quantity, now);
        await SaveAsync(userId, cancellationToken);

        return CartDto.From(cart);
    }

    public async Task<CartDto> ChangeQuantityAsync(
        string userId, string productId, ChangeQuantityRequest request, CancellationToken cancellationToken)
    {
        await changeQuantityValidator.ValidateAndThrowAsync(request, cancellationToken);

        var cart = await GetExistingCartAsync(userId, productId, cancellationToken);
        cart.ChangeQuantity(productId, request.Quantity, timeProvider.GetUtcNow());
        await SaveAsync(userId, cancellationToken);

        return CartDto.From(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string userId, string productId, CancellationToken cancellationToken)
    {
        var cart = await GetExistingCartAsync(userId, productId, cancellationToken);
        cart.RemoveItem(productId, timeProvider.GetUtcNow());
        await SaveAsync(userId, cancellationToken);

        return CartDto.From(cart);
    }

    public async Task ClearAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
            return;

        cart.Clear(timeProvider.GetUtcNow());
        await SaveAsync(userId, cancellationToken);
    }

    private async Task SaveAsync(string userId, CancellationToken cancellationToken)
    {
        await repository.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(userId, CancellationToken.None);
    }

    // Without a cart there is no item to change or remove, so the caller gets the same not found error.
    private async Task<ShoppingCart> GetExistingCartAsync(string userId, string productId, CancellationToken cancellationToken) =>
        await repository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new CartItemNotFoundException(productId);
}
