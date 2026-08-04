namespace ElectronicLibrary.BLL.Models.Storage;

public sealed class FileUploadData
{
    public Stream Content { get; init; } = Stream.Null;

    public string FileName { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public long Length { get; init; }
}