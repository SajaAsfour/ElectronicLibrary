namespace ElectronicLibrary.DAL.Models.Catalog;

public class Author
{
    public int AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string? Biography { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}