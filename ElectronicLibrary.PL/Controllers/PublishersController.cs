using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Publishers;
using ElectronicLibrary.PL.Authorization;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/publishers")]
public class PublishersController : ControllerBase
{
    private readonly IPublisherService _publisherService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public PublishersController(
        IPublisherService publisherService,
        IStringLocalizer<SharedResources> localizer)
    {
        _publisherService = publisherService;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedResponse<PublisherResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PublisherResponse>>>
        GetPublishers(
            [FromQuery] PublisherQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        PagedResponse<PublisherResponse> response =
            await _publisherService.GetPublishersAsync(
                queryParameters,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{publisherId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PublisherDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublisherDetailsResponse>>
        GetPublisherById(
            int publisherId,
            CancellationToken cancellationToken)
    {
        PublisherDetailsResponse response =
            await _publisherService.GetPublisherByIdAsync(
                publisherId,
                cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(PublisherResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublisherResponse>>
        CreatePublisher(
            [FromBody] CreatePublisherRequest request,
            CancellationToken cancellationToken)
    {
        PublisherResponse response =
            await _publisherService.CreatePublisherAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetPublisherById),
            new
            {
                publisherId = response.PublisherId
            },
            response);
    }

    [HttpPut("{publisherId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(PublisherResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublisherResponse>>
        UpdatePublisher(
            int publisherId,
            [FromBody] UpdatePublisherRequest request,
            CancellationToken cancellationToken)
    {
        PublisherResponse response =
            await _publisherService.UpdatePublisherAsync(
                publisherId,
                request,
                cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{publisherId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>>
        DeletePublisher(
            int publisherId,
            CancellationToken cancellationToken)
    {
        await _publisherService.DeletePublisherAsync(
            publisherId,
            cancellationToken);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "PublisherDeletedSuccessfully"].Value
        });
    }
}