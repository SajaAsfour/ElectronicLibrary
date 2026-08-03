namespace ElectronicLibrary.DAL.DTOs.Responses.Publishers;

public class PublisherDetailsResponse
{
    public int PublisherId { get; set; }

    public string Name { get; set; } = null!;

    public string? Website { get; set; }

    public DateTime CreatedAt { get; set; }

    public IReadOnlyCollection<PublisherBookResponse> Books { get; set; }
        = Array.Empty<PublisherBookResponse>();
}