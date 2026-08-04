namespace ElectronicLibrary.DAL.DTOs.Responses.Books;

public class BookDetailsResponse
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Isbn { get; set; }

    public string? Description { get; set; }

    public string Language { get; set; } = null!;

    public int? PublicationYear { get; set; }

    public DateTime CreatedAt { get; set; }

    public BookPublisherResponse Publisher { get; set; }
        = null!;

    public IReadOnlyCollection<BookAuthorResponse> Authors { get; set; }
        = Array.Empty<BookAuthorResponse>();

    public IReadOnlyCollection<BookCategoryResponse> Categories { get; set; }
        = Array.Empty<BookCategoryResponse>();

    public IReadOnlyCollection<BookImageResponse> Images { get; set; }
        = Array.Empty<BookImageResponse>();

    public int AvailableListingsCount { get; set; }
}