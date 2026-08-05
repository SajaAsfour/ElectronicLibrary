using ElectronicLibrary.BLL.Interfaces.Marketplace;
using ElectronicLibrary.DAL.DTOs.Requests.Listings;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Listings;
using ElectronicLibrary.PL.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingsController(
        IListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpPost]
    [Authorize(
        Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(ListingResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>>
        CreateListing(
            [FromBody] CreateListingRequest request,
            CancellationToken cancellationToken)
    {
        ListingResponse response =
            await _listingService
                .CreateListingAsync(
                    request,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpGet("{listingId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ListingResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>>
        GetListingById(
            int listingId,
            CancellationToken cancellationToken)
    {
        ListingResponse response =
            await _listingService
                .GetListingByIdAsync(
                    listingId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{listingId:int}")]
    [Authorize(
        Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(ListingResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>>
        UpdateListing(
            int listingId,
            [FromBody] UpdateListingRequest request,
            CancellationToken cancellationToken)
    {
        ListingResponse response =
            await _listingService
                .UpdateListingAsync(
                    listingId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{listingId:int}/status")]
    [Authorize(
        Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(ListingResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>>
        UpdateListingStatus(
            int listingId,
            [FromBody] UpdateListingStatusRequest request,
            CancellationToken cancellationToken)
    {
        ListingResponse response =
            await _listingService
                .UpdateListingStatusAsync(
                    listingId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{listingId:int}")]
    [Authorize(
        Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteListing(
        int listingId,
        CancellationToken cancellationToken)
    {
        await _listingService.DeleteListingAsync(
            listingId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("mine")]
    [Authorize(
        Policy = AuthorizationPolicyNames.SellerOnly)]
    [ProducesResponseType(
        typeof(PagedResponse<ListingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        PagedResponse<ListingResponse>>>
        GetCurrentSellerListings(
            [FromQuery]
            SellerListingFilterRequest request,
            CancellationToken cancellationToken)
    {
        PagedResponse<ListingResponse> response =
            await _listingService
                .GetCurrentSellerListingsAsync(
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpGet("/api/books/{bookId:int}/listings")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedResponse<ListingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        PagedResponse<ListingResponse>>>
        GetBookListings(
            int bookId,
            [FromQuery]
            BookListingFilterRequest request,
            CancellationToken cancellationToken)
    {
        PagedResponse<ListingResponse> response =
            await _listingService
                .GetBookListingsAsync(
                    bookId,
                    request,
                    cancellationToken);

        return Ok(response);
    }
}