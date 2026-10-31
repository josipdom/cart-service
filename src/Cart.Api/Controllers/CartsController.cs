using System.Security.Claims;
using Cart.Application.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/carts/me")]
public sealed class CartsController(ShoppingCartService cartService) : ControllerBase
{
    [HttpGet]
    public Task<CartDto> Get(CancellationToken cancellationToken) =>
        cartService.GetAsync(UserId, cancellationToken);

    [HttpPost("items")]
    public Task<CartDto> AddItem(AddItemRequest request, CancellationToken cancellationToken) =>
        cartService.AddItemAsync(UserId, request, cancellationToken);

    [HttpPut("items/{productId}")]
    public Task<CartDto> ChangeQuantity(string productId, ChangeQuantityRequest request, CancellationToken cancellationToken) =>
        cartService.ChangeQuantityAsync(UserId, productId, request, cancellationToken);

    [HttpDelete("items/{productId}")]
    public Task<CartDto> RemoveItem(string productId, CancellationToken cancellationToken) =>
        cartService.RemoveItemAsync(UserId, productId, cancellationToken);

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await cartService.ClearAsync(UserId, cancellationToken);
        return NoContent();
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("The token has no 'sub' claim.");
}
