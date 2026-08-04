using ElectronicLibrary.BLL.Interfaces.Sellers;
using ElectronicLibrary.DAL.DTOs.Requests.Sellers;
using ElectronicLibrary.DAL.DTOs.Responses.Sellers;
using ElectronicLibrary.PL.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/sellers")]
public class SellersController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public SellersController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    [HttpPost("activate")]
    [Authorize]
    [ProducesResponseType(
        typeof(SellerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SellerProfileResponse>>
        ActivateSeller(
            [FromBody] ActivateSellerRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _sellerService.ActivateSellerAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(SellerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SellerProfileResponse>>
        GetCurrentSellerProfile(
            CancellationToken cancellationToken)
    {
        var response =
            await _sellerService.GetCurrentSellerProfileAsync(
                cancellationToken);

        return Ok(response);
    }

    [HttpPut("me")]
    [Authorize(Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(SellerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SellerProfileResponse>>
        UpdateCurrentSellerProfile(
            [FromBody] UpdateSellerProfileRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _sellerService.UpdateCurrentSellerProfileAsync(
                request,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{sellerId}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PublicSellerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicSellerProfileResponse>>
        GetPublicSellerProfile(
            string sellerId,
            CancellationToken cancellationToken)
    {
        var response =
            await _sellerService.GetPublicSellerProfileAsync(
                sellerId,
                cancellationToken);

        return Ok(response);
    }
}
