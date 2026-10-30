using Cart.Domain.Events;
using Cart.Domain.Exceptions;

namespace Cart.Domain;

public sealed class ShoppingCart
{
    public const int MaxQuantityPerItem = 99;
    public const int MaxDistinctItems = 50;

    private readonly List<CartItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private ShoppingCart(Guid id, string userId, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int Version { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public decimal Total => _items.Sum(i => i.LineTotal);

    public static ShoppingCart Create(string userId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User id is required.");

        // Version 7 GUIDs are time-ordered, which keeps primary key index inserts sequential.
        return new ShoppingCart(Guid.CreateVersion7(now), userId, now);
    }

    public void AddItem(string productId, string productName, decimal unitPrice, int quantity, DateTimeOffset now)
    {
        EnsureValidQuantity(quantity);

        var item = FindItem(productId);
        if (item is null)
        {
            if (_items.Count >= MaxDistinctItems)
                throw new DomainException($"A cart can contain at most {MaxDistinctItems} different products.");

            item = new CartItem(productId, productName, unitPrice, quantity);
            _items.Add(item);
        }
        else
        {
            var newQuantity = item.Quantity + quantity;
            if (newQuantity > MaxQuantityPerItem)
                throw new DomainException($"Quantity of product '{productId}' cannot exceed {MaxQuantityPerItem}.");

            item.UpdateProductDetails(productName, unitPrice);
            item.SetQuantity(newQuantity);
        }

        Record(new CartItemAdded(Id, UserId, productId, quantity, item.Quantity, item.UnitPrice, now));
    }

    public void ChangeQuantity(string productId, int quantity, DateTimeOffset now)
    {
        EnsureValidQuantity(quantity);

        var item = GetItem(productId);
        if (item.Quantity == quantity)
            return;

        var oldQuantity = item.Quantity;
        item.SetQuantity(quantity);

        Record(new CartItemQuantityChanged(Id, UserId, productId, oldQuantity, quantity, now));
    }

    public void RemoveItem(string productId, DateTimeOffset now)
    {
        var item = GetItem(productId);
        _items.Remove(item);

        Record(new CartItemRemoved(Id, UserId, productId, now));
    }

    public void Clear(DateTimeOffset now)
    {
        if (_items.Count == 0)
            return;

        _items.Clear();

        Record(new CartCleared(Id, UserId, now));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Record(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        Version++;
        UpdatedAt = domainEvent.OccurredAt;
    }

    private CartItem? FindItem(string productId) =>
        _items.FirstOrDefault(i => i.ProductId == productId);

    private CartItem GetItem(string productId) =>
        FindItem(productId) ?? throw new CartItemNotFoundException(productId);

    private static void EnsureValidQuantity(int quantity)
    {
        if (quantity is < 1 or > MaxQuantityPerItem)
            throw new DomainException($"Quantity must be between 1 and {MaxQuantityPerItem}.");
    }
}
