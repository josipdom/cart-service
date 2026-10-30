using Cart.Domain;
using FluentValidation;

namespace Cart.Application.Carts;

public sealed record AddItemRequest(string ProductId, int Quantity);

public sealed class AddItemRequestValidator : AbstractValidator<AddItemRequest>
{
    public AddItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).InclusiveBetween(1, ShoppingCart.MaxQuantityPerItem);
    }
}
