namespace ElectronicLibrary.DAL.DTOs.Responses.Authors;

public class AuthorDetailsResponse
{
    public int AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string? Biography { get; set; }

    public DateTime CreatedAt { get; set; }

    public IReadOnlyCollection<AuthorBookResponse> Books { get; set; }
        = Array.Empty<AuthorBookResponse>();
}