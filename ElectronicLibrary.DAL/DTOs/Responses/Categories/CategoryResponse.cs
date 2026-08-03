namespace ElectronicLibrary.DAL.DTOs.Responses.Categories;

public class CategoryResponse
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int BooksCount { get; set; }
}