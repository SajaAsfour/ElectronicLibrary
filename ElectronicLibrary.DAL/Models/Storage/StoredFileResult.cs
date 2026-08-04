namespace ElectronicLibrary.BLL.Models.Storage;

public sealed class StoredFileResult
{
    public string FileName { get; init; } = null!;

    public string RelativePath { get; init; } = null!;

    public string PublicUrl { get; init; } = null!;
}