namespace ElectronicLibrary.DAL.DTOs.Responses.Authors;

public class AuthorResponse
{
    public int AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string? Biography { get; set; }

    public int BooksCount { get; set; }
}