using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Common;

namespace ElectronicLibrary.BLL.Interfaces.Catalog;

public interface IAuthorService
{
    Task<PagedResponse<AuthorResponse>> GetAuthorsAsync(
        AuthorQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<AuthorDetailsResponse> GetAuthorByIdAsync(
        int authorId,
        CancellationToken cancellationToken = default);

    Task<AuthorResponse> CreateAuthorAsync(
        CreateAuthorRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthorResponse> UpdateAuthorAsync(
        int authorId,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAuthorAsync(
        int authorId,
        CancellationToken cancellationToken = default);
}