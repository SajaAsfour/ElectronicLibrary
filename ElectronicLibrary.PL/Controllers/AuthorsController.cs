using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.PL.Authorization;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public AuthorsController(
        IAuthorService authorService,
        IStringLocalizer<SharedResources> localizer)
    {
        _authorService = authorService;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedResponse<AuthorResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AuthorResponse>>>
        GetAuthors(
            [FromQuery] AuthorQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        var response = await _authorService.GetAuthorsAsync(
            queryParameters,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{authorId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(AuthorDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorDetailsResponse>>
        GetAuthorById(
            int authorId,
            CancellationToken cancellationToken)
    {
        var response = await _authorService.GetAuthorByIdAsync(
            authorId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(AuthorResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorResponse>>
        CreateAuthor(
            [FromBody] CreateAuthorRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _authorService.CreateAuthorAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAuthorById),
            new
            {
                authorId = response.AuthorId
            },
            response);
    }

    [HttpPut("{authorId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(AuthorResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorResponse>>
        UpdateAuthor(
            int authorId,
            [FromBody] UpdateAuthorRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _authorService.UpdateAuthorAsync(
            authorId,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{authorId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>>
        DeleteAuthor(
            int authorId,
            CancellationToken cancellationToken)
    {
        await _authorService.DeleteAuthorAsync(
            authorId,
            cancellationToken);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "AuthorDeletedSuccessfully"].Value
        });
    }
}