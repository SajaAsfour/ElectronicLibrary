namespace ElectronicLibrary.DAL.Models.Catalog;

public class BookImage
{
    public int BookImageId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsMain { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;
}