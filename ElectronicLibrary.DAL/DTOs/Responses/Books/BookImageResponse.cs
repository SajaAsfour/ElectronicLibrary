namespace ElectronicLibrary.DAL.DTOs.Responses.Books;

public class BookImageResponse
{
    public int BookImageId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsMain { get; set; }
}