namespace ElectronicLibrary.DAL.Models.Catalog;

public class Publisher
{
    public int PublisherId { get; set; }

    public string Name { get; set; } = null!;

    public string? Website { get; set; }

    public ICollection<Book> Books { get; set; } = [];
}