using ElectronicLibrary.BLL.Interfaces.Storage;
using ElectronicLibrary.BLL.Models.Storage;

namespace ElectronicLibrary.UnitTests.Helpers;

public sealed class FakeFileStorageService
    : IFileStorageService
{
    private readonly Dictionary<string, byte[]>
        _storedFiles =
            new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, byte[]>
        StoredFiles => _storedFiles;

    public List<string> DeletedPaths { get; } = [];

    public async Task<StoredFileResult>
        SaveBookImageAsync(
            int bookId,
            Stream fileStream,
            string originalFileName,
            string contentType,
            long fileLength,
            CancellationToken cancellationToken = default)
    {
        string extension =
            Path.GetExtension(originalFileName)
                .ToLowerInvariant();

        string fileName =
            $"{Guid.NewGuid():N}{extension}";

        string relativePath =
            $"uploads/books/{bookId}/{fileName}";

        using var memoryStream =
            new MemoryStream();

        await fileStream.CopyToAsync(
            memoryStream,
            cancellationToken);

        _storedFiles[relativePath] =
            memoryStream.ToArray();

        return new StoredFileResult
        {
            FileName = fileName,
            RelativePath = relativePath,
            PublicUrl = $"/{relativePath}"
        };
    }

    public Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedPath =
            relativePath
                .Trim()
                .TrimStart('/', '\\');

        _storedFiles.Remove(
            normalizedPath);

        DeletedPaths.Add(
            normalizedPath);

        return Task.CompletedTask;
    }
}