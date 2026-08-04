using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
using ElectronicLibrary.BLL.Models.Storage;

namespace ElectronicLibrary.BLL.Interfaces.Catalog;

public interface IBookService
{
    Task<IReadOnlyCollection<BookResponse>> GetBooksAsync(
        CancellationToken cancellationToken = default);

    Task<BookDetailsResponse> GetBookByIdAsync(
        int bookId,
        CancellationToken cancellationToken = default);

    Task<BookDetailsResponse> CreateBookAsync(
    CreateBookRequest request,
    IReadOnlyCollection<FileUploadData> images,
    int? mainImageIndex,
    CancellationToken cancellationToken = default);

    Task<BookDetailsResponse> UpdateBookAsync(
        int bookId,
        UpdateBookRequest request,
        IReadOnlyCollection<FileUploadData> images,
        int? mainImageIndex,
        CancellationToken cancellationToken = default);

    Task DeleteBookAsync(
        int bookId,
        CancellationToken cancellationToken = default);

    Task<UploadBookImagesResponse> UploadBookImagesAsync(
    int bookId,
    IReadOnlyCollection<FileUploadData> files,
    int? mainImageIndex,
    CancellationToken cancellationToken = default);

    Task<UploadBookImagesResponse> DeleteBookImageAsync(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken = default);

    Task<UploadBookImagesResponse> SetMainBookImageAsync(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken = default);
}