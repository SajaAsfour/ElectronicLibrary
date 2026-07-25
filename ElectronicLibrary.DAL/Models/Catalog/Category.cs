namespace ElectronicLibrary.DAL.Models.Catalog;

public class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<BookCategory> BookCategories { get; set; } = [];
}