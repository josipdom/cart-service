using Cart.Api.Auth;
using Cart.Api.RateLimiting;
using Cart.Application.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cart.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.PerUserPolicy)]
[Route("api/user/cart")]
public sealed class CartController(ShoppingCartService cartService) : ControllerBase
{
    /// <summary>Returns the current user's cart, or an empty cart if there is none yet.</summary>
    /// <response code="200">The cart.</response>
    /// <response code="401">Missing or invalid access token.</response>
    /// <response code="429">Too many requests from this user.</response>
    [HttpGet]
    public Task<CartDto> Get(CancellationToken cancellationToken) =>
        cartService.GetAsync(User.GetUserId(), cancellationToken);

    /// <summary>Adds a product to the cart. If it is already there, the quantities are added up.</summary>
    /// <response code="200">The updated cart.</response>
    /// <response code="400">Invalid product ID or quantity.</response>
    /// <response code="401">Missing or invalid access token.</response>
    /// <response code="404">The product does not exist in the catalog.</response>
    /// <response code="409">The cart was changed by another request at the same time.</response>
    /// <response code="422">A cart rule was broken, e.g. more than 99 pieces or 50 products.</response>
    /// <response code="429">Too many requests from this user.</response>
    [HttpPost("items")]
    public Task<CartDto> AddItem(AddItemRequest request, CancellationToken cancellationToken) =>
        cartService.AddItemAsync(User.GetUserId(), request, cancellationToken);

    /// <summary>Sets the quantity of a product that is already in the cart.</summary>
    /// <response code="200">The updated cart.</response>
    /// <response code="400">Invalid quantity.</response>
    /// <response code="401">Missing or invalid access token.</response>
    /// <response code="404">The product is not in the cart.</response>
    /// <response code="409">The cart was changed by another request at the same time.</response>
    /// <response code="429">Too many requests from this user.</response>
    [HttpPut("items/{productId}")]
    public Task<CartDto> ChangeQuantity(string productId, ChangeQuantityRequest request, CancellationToken cancellationToken) =>
        cartService.ChangeQuantityAsync(User.GetUserId(), productId, request, cancellationToken);

    /// <summary>Removes a product from the cart.</summary>
    /// <response code="200">The updated cart.</response>
    /// <response code="401">Missing or invalid access token.</response>
    /// <response code="404">The product is not in the cart.</response>
    /// <response code="409">The cart was changed by another request at the same time.</response>
    /// <response code="429">Too many requests from this user.</response>
    [HttpDelete("items/{productId}")]
    public Task<CartDto> RemoveItem(string productId, CancellationToken cancellationToken) =>
        cartService.RemoveItemAsync(User.GetUserId(), productId, cancellationToken);

    /// <summary>Removes all products from the cart.</summary>
    /// <response code="204">The cart is empty.</response>
    /// <response code="401">Missing or invalid access token.</response>
    /// <response code="409">The cart was changed by another request at the same time.</response>
    /// <response code="429">Too many requests from this user.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await cartService.ClearAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
