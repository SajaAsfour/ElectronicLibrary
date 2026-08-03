namespace ElectronicLibrary.DAL.DTOs.Responses.Publishers;

public class PublisherResponse
{
    public int PublisherId { get; set; }

    public string Name { get; set; } = null!;

    public string? Website { get; set; }

    public int BooksCount { get; set; }
}