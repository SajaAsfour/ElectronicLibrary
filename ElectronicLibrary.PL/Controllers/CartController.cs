using ElectronicLibrary.BLL.Interfaces.Shopping;
using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using ElectronicLibrary.DAL.DTOs.Responses.Carts;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IStringLocalizer<SharedResources>
        _localizer;

    public CartController(
        ICartService cartService,
        IStringLocalizer<SharedResources> localizer)
    {
        _cartService = cartService;
        _localizer = localizer;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>>
        GetCurrentUserCart(
            CancellationToken cancellationToken)
    {
        CartResponse response =
            await _cartService
                .GetCurrentUserCartAsync(
                    cancellationToken);

        return Ok(
            PrepareResponse(response));
    }

    [HttpPost("items")]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>>
        AddCartItem(
            [FromBody] AddCartItemRequest request,
            CancellationToken cancellationToken)
    {
        CartResponse response =
            await _cartService
                .AddCartItemAsync(
                    request,
                    cancellationToken);

        return Ok(
            PrepareResponse(response));
    }

    [HttpPut("items/{listingId:int}")]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>>
        UpdateCartItemQuantity(
            int listingId,
            [FromBody]
            UpdateCartItemQuantityRequest request,
            CancellationToken cancellationToken)
    {
        CartResponse response =
            await _cartService
                .UpdateCartItemQuantityAsync(
                    listingId,
                    request,
                    cancellationToken);

        return Ok(
            PrepareResponse(response));
    }

    [HttpDelete("items/{listingId:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        RemoveCartItem(
            int listingId,
            CancellationToken cancellationToken)
    {
        await _cartService
            .RemoveCartItemAsync(
                listingId,
                cancellationToken);

        return NoContent();
    }

    [HttpDelete("items")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        ClearCart(
            CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(
            cancellationToken);

        return NoContent();
    }

    private CartResponse PrepareResponse(
        CartResponse response)
    {
        foreach (CartItemResponse item in
                 response.Items)
        {
            if (!string.IsNullOrWhiteSpace(
                    item.MainImageUrl))
            {
                item.MainImageUrl =
                    ToAbsoluteUrl(
                        item.MainImageUrl);
            }

            if (!string.IsNullOrWhiteSpace(
                    item.AvailabilityMessage))
            {
                item.AvailabilityMessage =
                    GetLocalizedMessage(
                        item.AvailabilityMessage);
            }
        }

        return response;
    }

    private string GetLocalizedMessage(
        string resourceKey)
    {
        var localizedValue =
            _localizer[resourceKey];

        return localizedValue.ResourceNotFound
            ? resourceKey
            : localizedValue.Value;
    }

    private string ToAbsoluteUrl(
        string imageUrl)
    {
        if (Uri.TryCreate(
                imageUrl,
                UriKind.Absolute,
                out _))
        {
            return imageUrl;
        }

        string normalizedPath =
            imageUrl.StartsWith(
                "/",
                StringComparison.Ordinal)
                ? imageUrl
                : $"/{imageUrl}";

        return
            $"{Request.Scheme}://" +
            $"{Request.Host}" +
            $"{Request.PathBase}" +
            $"{normalizedPath}";
    }
}
