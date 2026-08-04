namespace ElectronicLibrary.DAL.DTOs.Responses.Books;

public class BookResponse
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Isbn { get; set; }

    public string Language { get; set; } = null!;

    public int? PublicationYear { get; set; }

    public string PublisherName { get; set; } = null!;

    public IReadOnlyCollection<string> Authors { get; set; }
        = Array.Empty<string>();

    public IReadOnlyCollection<string> Categories { get; set; }
        = Array.Empty<string>();

    public string? MainImageUrl { get; set; }

    public int AvailableListingsCount { get; set; }
}