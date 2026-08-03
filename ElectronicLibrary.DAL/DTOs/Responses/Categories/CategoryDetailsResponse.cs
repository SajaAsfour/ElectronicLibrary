namespace ElectronicLibrary.DAL.DTOs.Responses.Categories;

public class CategoryDetailsResponse
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public IReadOnlyCollection<CategoryBookResponse> Books { get; set; }
        = Array.Empty<CategoryBookResponse>();
}