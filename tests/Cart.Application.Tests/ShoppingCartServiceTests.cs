using Cart.Application.Abstractions;
using Cart.Application.Carts;
using Cart.Application.Exceptions;
using Cart.Domain;
using Cart.Domain.Exceptions;
using FluentValidation;
using NSubstitute;

namespace Cart.Application.Tests;

public class ShoppingCartServiceTests
{
    private const string UserId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
    private const string Keyboard = "SKU-1001";
    private const string Mouse = "SKU-1002";
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly ICartRepository _repository = Substitute.For<ICartRepository>();
    private readonly ICartCache _cache = Substitute.For<ICartCache>();
    private readonly IProductCatalog _catalog = Substitute.For<IProductCatalog>();
    private readonly ShoppingCartService _service;

    public ShoppingCartServiceTests()
    {
        _catalog.GetProductAsync(Keyboard, Arg.Any<CancellationToken>())
            .Returns(new ProductInfo(Keyboard, "Mechanical keyboard", 49.99m));
        _catalog.GetProductAsync(Mouse, Arg.Any<CancellationToken>())
            .Returns(new ProductInfo(Mouse, "Wireless mouse", 19.50m));

        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);

        _service = new ShoppingCartService(
            _repository,
            _cache,
            _catalog,
            new AddItemRequestValidator(),
            new ChangeQuantityRequestValidator(),
            timeProvider);
    }

    [Fact]
    public async Task GetAsync_NoCart_ReturnsEmptyCart()
    {
        var cart = await _service.GetAsync(UserId, CancellationToken.None);

        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Total);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CartInCache_ReturnsItWithoutReadingRepository()
    {
        var cached = new CartDto([new CartItemDto(Keyboard, "Mechanical keyboard", 49.99m, 1, 49.99m)], 49.99m, "EUR");
        _cache.GetAsync(UserId, Arg.Any<CancellationToken>()).Returns(cached);

        var cart = await _service.GetAsync(UserId, CancellationToken.None);

        Assert.Same(cached, cart);
        await _repository.DidNotReceive().GetByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CartNotInCache_ReadsRepositoryAndCachesResult()
    {
        GivenExistingCart();

        var cart = await _service.GetAsync(UserId, CancellationToken.None);

        Assert.Single(cart.Items);
        await _cache.Received(1).SetAsync(UserId, cart, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_Saved_RemovesCartFromCache()
    {
        await _service.AddItemAsync(UserId, new AddItemRequest(Keyboard, 1), CancellationToken.None);

        Received.InOrder(() =>
        {
            _repository.SaveChangesAsync(Arg.Any<CancellationToken>());
            _cache.RemoveAsync(UserId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task AddItemAsync_SaveFails_KeepsCache()
    {
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyConflictException(new Exception())));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.AddItemAsync(UserId, new AddItemRequest(Keyboard, 1), CancellationToken.None));

        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_NoCart_CreatesCartWithCatalogNameAndPrice()
    {
        var cart = await _service.AddItemAsync(UserId, new AddItemRequest(Keyboard, 2), CancellationToken.None);

        var item = Assert.Single(cart.Items);
        Assert.Equal("Mechanical keyboard", item.ProductName);
        Assert.Equal(49.99m, item.UnitPrice);
        Assert.Equal(99.98m, cart.Total);
        _repository.Received(1).Add(Arg.Is<ShoppingCart>(c => c.UserId == UserId));
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_ExistingCart_AddsToSameCart()
    {
        var existing = GivenExistingCart();

        await _service.AddItemAsync(UserId, new AddItemRequest(Mouse, 1), CancellationToken.None);

        Assert.Equal(2, existing.Items.Count);
        _repository.DidNotReceive().Add(Arg.Any<ShoppingCart>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    }

    [Fact]
    public async Task AddItemAsync_ProductNotInCatalog_ThrowsProductNotFound()
    {
        var request = new AddItemRequest("SKU-UNKNOWN", 1);

        var exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _service.AddItemAsync(UserId, request, CancellationToken.None));

        Assert.Equal("SKU-UNKNOWN", exception.ProductId);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData(Keyboard, 0)]
    [InlineData(Keyboard, 100)]
    public async Task AddItemAsync_InvalidRequest_ThrowsValidationException(string productId, int quantity)
    {
        var request = new AddItemRequest(productId, quantity);

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.AddItemAsync(UserId, request, CancellationToken.None));

        await _catalog.DidNotReceive().GetProductAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeQuantityAsync_ExistingItem_UpdatesQuantityAndSaves()
    {
        GivenExistingCart();

        var cart = await _service.ChangeQuantityAsync(UserId, Keyboard, new ChangeQuantityRequest(4), CancellationToken.None);

        Assert.Equal(4, Assert.Single(cart.Items).Quantity);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeQuantityAsync_NoCart_ThrowsCartItemNotFound()
    {
        await Assert.ThrowsAsync<CartItemNotFoundException>(
            () => _service.ChangeQuantityAsync(UserId, Keyboard, new ChangeQuantityRequest(2), CancellationToken.None));
    }

    [Fact]
    public async Task ChangeQuantityAsync_InvalidQuantity_ThrowsValidationException()
    {
        GivenExistingCart();

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.ChangeQuantityAsync(UserId, Keyboard, new ChangeQuantityRequest(0), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesItAndSaves()
    {
        GivenExistingCart();

        var cart = await _service.RemoveItemAsync(UserId, Keyboard, CancellationToken.None);

        Assert.Empty(cart.Items);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_NoCart_ThrowsCartItemNotFound()
    {
        await Assert.ThrowsAsync<CartItemNotFoundException>(
            () => _service.RemoveItemAsync(UserId, Keyboard, CancellationToken.None));
    }

    [Fact]
    public async Task ClearAsync_NoCart_DoesNotSave()
    {
        await _service.ClearAsync(UserId, CancellationToken.None);

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAsync_ExistingCart_ClearsItAndSaves()
    {
        var existing = GivenExistingCart();

        await _service.ClearAsync(UserId, CancellationToken.None);

        Assert.Empty(existing.Items);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(UserId, Arg.Any<CancellationToken>());
    }

    private ShoppingCart GivenExistingCart()
    {
        var cart = ShoppingCart.Create(UserId, Now);
        cart.AddItem(Keyboard, "Mechanical keyboard", 49.99m, 1, Now);
        _repository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(cart);
        return cart;
    }
}
