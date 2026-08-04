namespace ElectronicLibrary.BLL.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootFolder { get; set; } = "uploads";

    public string BooksFolder { get; set; } = "books";

    public long MaxImageSizeInBytes { get; set; } =
        5 * 1024 * 1024;

    public string[] AllowedImageExtensions { get; set; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];
}