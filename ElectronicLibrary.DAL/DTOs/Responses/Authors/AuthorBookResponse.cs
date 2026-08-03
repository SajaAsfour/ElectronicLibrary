namespace ElectronicLibrary.DAL.DTOs.Responses.Authors;

public class AuthorBookResponse
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Isbn { get; set; }

    public string Language { get; set; } = null!;

    public int? PublicationYear { get; set; }
}