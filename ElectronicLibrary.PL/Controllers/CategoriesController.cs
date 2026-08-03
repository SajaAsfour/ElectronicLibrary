using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.DAL.DTOs.Requests.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.PL.Authorization;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public CategoriesController(
        ICategoryService categoryService,
        IStringLocalizer<SharedResources> localizer)
    {
        _categoryService = categoryService;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedResponse<CategoryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>>
        GetCategories(
            [FromQuery] CategoryQueryParameters queryParameters,
            CancellationToken cancellationToken)
    {
        PagedResponse<CategoryResponse> response =
            await _categoryService.GetCategoriesAsync(
                queryParameters,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{categoryId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(CategoryDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsResponse>>
        GetCategoryById(
            int categoryId,
            CancellationToken cancellationToken)
    {
        CategoryDetailsResponse response =
            await _categoryService.GetCategoryByIdAsync(
                categoryId,
                cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>>
        CreateCategory(
            [FromBody] CreateCategoryRequest request,
            CancellationToken cancellationToken)
    {
        CategoryResponse response =
            await _categoryService.CreateCategoryAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetCategoryById),
            new
            {
                categoryId = response.CategoryId
            },
            response);
    }

    [HttpPut("{categoryId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>>
        UpdateCategory(
            int categoryId,
            [FromBody] UpdateCategoryRequest request,
            CancellationToken cancellationToken)
    {
        CategoryResponse response =
            await _categoryService.UpdateCategoryAsync(
                categoryId,
                request,
                cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{categoryId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>>
        DeleteCategory(
            int categoryId,
            CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(
            categoryId,
            cancellationToken);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "CategoryDeletedSuccessfully"].Value
        });
    }
}