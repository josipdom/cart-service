using Cart.Domain;
using FluentValidation;

namespace Cart.Application.Carts;

public sealed record ChangeQuantityRequest(int Quantity);

public sealed class ChangeQuantityRequestValidator : AbstractValidator<ChangeQuantityRequest>
{
    public ChangeQuantityRequestValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, ShoppingCart.MaxQuantityPerItem);
    }
}
