using ElectronicLibrary.BLL.Models.Storage;

namespace ElectronicLibrary.BLL.Models.Catalog;

public sealed class CreateBookCommand
{
    public string Title { get; init; } = null!;

    public string? Isbn { get; init; }

    public string? Description { get; init; }

    public string Language { get; init; } = null!;

    public int? PublicationYear { get; init; }

    public int PublisherId { get; init; }

    public IReadOnlyCollection<int> AuthorIds { get; init; }
        = Array.Empty<int>();

    public IReadOnlyCollection<int> CategoryIds { get; init; }
        = Array.Empty<int>();

    public IReadOnlyCollection<FileUploadData> Images { get; init; }
        = Array.Empty<FileUploadData>();

    public int? MainImageIndex { get; init; }
}