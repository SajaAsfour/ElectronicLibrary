using ElectronicLibrary.BLL.Interfaces.Storage;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.BLL.Options;
using Microsoft.Extensions.Options;

namespace ElectronicLibrary.PL.Services.Storage;

public sealed class LocalFileStorageService
    : IFileStorageService
{
    private static readonly IReadOnlyDictionary<string, string[]>
        AllowedContentTypes =
            new Dictionary<string, string[]>(
                StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] =
                [
                    "image/jpeg"
                ],
                [".jpeg"] =
                [
                    "image/jpeg"
                ],
                [".png"] =
                [
                    "image/png"
                ],
                [".webp"] =
                [
                    "image/webp"
                ]
            };

    private readonly IWebHostEnvironment _environment;
    private readonly FileStorageOptions _options;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IOptions<FileStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<StoredFileResult>
        SaveBookImageAsync(
            int bookId,
            Stream fileStream,
            string originalFileName,
            string contentType,
            long fileLength,
            CancellationToken cancellationToken = default)
    {
        ValidateBookId(bookId);

        string extension =
            ValidateFile(
                originalFileName,
                contentType,
                fileLength);

        string webRootPath =
            EnsureWebRootPath();

        string bookFolderPath =
            Path.Combine(
                webRootPath,
                _options.RootFolder,
                _options.BooksFolder,
                bookId.ToString());

        Directory.CreateDirectory(
            bookFolderPath);

        string generatedFileName =
            $"{Guid.NewGuid():N}{extension}";

        string fullFilePath =
            Path.Combine(
                bookFolderPath,
                generatedFileName);

        try
        {
            await using var outputStream =
                new FileStream(
                    fullFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

            await fileStream.CopyToAsync(
                outputStream,
                cancellationToken);
        }
        catch
        {
            if (File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath);
            }

            throw;
        }

        string relativePath =
            Path.Combine(
                    _options.RootFolder,
                    _options.BooksFolder,
                    bookId.ToString(),
                    generatedFileName)
                .Replace(
                    Path.DirectorySeparatorChar,
                    '/');

        return new StoredFileResult
        {
            FileName = generatedFileName,
            RelativePath = relativePath,
            PublicUrl = $"/{relativePath}"
        };
    }

    public Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        string normalizedRelativePath =
            relativePath
                .Trim()
                .TrimStart('/', '\\')
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar)
                .Replace(
                    '\\',
                    Path.DirectorySeparatorChar);

        string webRootPath =
            EnsureWebRootPath();

        string fullFilePath =
            Path.GetFullPath(
                Path.Combine(
                    webRootPath,
                    normalizedRelativePath));

        string allowedRootPath =
            Path.GetFullPath(
                Path.Combine(
                    webRootPath,
                    _options.RootFolder,
                    _options.BooksFolder));

        bool isInsideAllowedFolder =
            fullFilePath.StartsWith(
                allowedRootPath +
                Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);

        if (!isInsideAllowedFolder)
        {
            throw new InvalidOperationException(
                "InvalidBookImagePath");
        }

        if (File.Exists(fullFilePath))
        {
            File.Delete(fullFilePath);
        }

        DeleteEmptyParentFolder(
            fullFilePath,
            allowedRootPath);

        return Task.CompletedTask;
    }

    private string ValidateFile(
        string originalFileName,
        string contentType,
        long fileLength)
    {
        if (fileLength <= 0)
        {
            throw new InvalidOperationException(
                "BookImageFileRequired");
        }

        if (fileLength >
            _options.MaxImageSizeInBytes)
        {
            throw new InvalidOperationException(
                "BookImageFileTooLarge");
        }

        string safeFileName =
            Path.GetFileName(
                originalFileName);

        string extension =
            Path.GetExtension(
                    safeFileName)
                .ToLowerInvariant();

        bool extensionAllowed =
            _options.AllowedImageExtensions.Any(
                allowedExtension =>
                    string.Equals(
                        allowedExtension,
                        extension,
                        StringComparison.OrdinalIgnoreCase));

        if (!extensionAllowed)
        {
            throw new InvalidOperationException(
                "BookImageFileTypeNotAllowed");
        }

        if (!AllowedContentTypes.TryGetValue(
                extension,
                out string[]? validContentTypes))
        {
            throw new InvalidOperationException(
                "BookImageFileTypeNotAllowed");
        }

        bool contentTypeAllowed =
            validContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase);

        if (!contentTypeAllowed)
        {
            throw new InvalidOperationException(
                "BookImageFileTypeNotAllowed");
        }

        return extension;
    }

    private string EnsureWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(
                _environment.WebRootPath))
        {
            Directory.CreateDirectory(
                _environment.WebRootPath);

            return _environment.WebRootPath;
        }

        string webRootPath =
            Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");

        Directory.CreateDirectory(
            webRootPath);

        return webRootPath;
    }

    private static void ValidateBookId(
        int bookId)
    {
        if (bookId <= 0)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }
    }

    private static void DeleteEmptyParentFolder(
        string fullFilePath,
        string allowedRootPath)
    {
        string? parentFolder =
            Path.GetDirectoryName(
                fullFilePath);

        if (string.IsNullOrWhiteSpace(parentFolder) ||
            !Directory.Exists(parentFolder))
        {
            return;
        }

        if (Directory.EnumerateFileSystemEntries(
                parentFolder)
            .Any())
        {
            return;
        }

        string normalizedParentFolder =
            Path.GetFullPath(parentFolder);

        string normalizedAllowedRoot =
            Path.GetFullPath(allowedRootPath);

        if (string.Equals(
                normalizedParentFolder,
                normalizedAllowedRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Directory.Delete(parentFolder);
    }
}