using Cart.Application.Carts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cart.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ShoppingCartService>();
        services.AddSingleton<IValidator<AddItemRequest>, AddItemRequestValidator>();
        services.AddSingleton<IValidator<ChangeQuantityRequest>, ChangeQuantityRequestValidator>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
