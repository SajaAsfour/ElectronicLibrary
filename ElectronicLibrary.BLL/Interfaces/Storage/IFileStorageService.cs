using ElectronicLibrary.BLL.Models.Storage;

namespace ElectronicLibrary.BLL.Interfaces.Storage;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveBookImageAsync(
        int bookId,
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileLength,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default);
}