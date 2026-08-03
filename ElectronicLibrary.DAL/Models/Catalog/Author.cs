namespace ElectronicLibrary.DAL.Models.Catalog;

public class Author
{
    public int AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string? Biography { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedById { get; set; }

    public string? UpdatedById { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedById { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}