namespace ElectronicLibrary.DAL.DTOs.Responses.Books;

public class BookPublisherResponse
{
    public int PublisherId { get; set; }

    public string Name { get; set; } = null!;

    public string? Website { get; set; }
}