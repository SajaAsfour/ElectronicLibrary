using ElectronicLibrary.DAL.DTOs.Requests.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Common;

namespace ElectronicLibrary.BLL.Interfaces.Catalog;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(
        CategoryQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<CategoryDetailsResponse> GetCategoryByIdAsync(
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryResponse> UpdateCategoryAsync(
        int categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default);
}