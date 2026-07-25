namespace ElectronicLibrary.DAL.Models.Catalog;

public class BookAuthor
{
    public int BookId { get; set; }

    public int AuthorId { get; set; }

    public Book Book { get; set; } = null!;

    public Author Author { get; set; } = null!;
}