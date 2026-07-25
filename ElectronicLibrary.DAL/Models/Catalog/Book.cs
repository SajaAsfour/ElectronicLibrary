using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Reviews;

namespace ElectronicLibrary.DAL.Models.Catalog;

public class Book
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? Isbn { get; set; }

    public string? Description { get; set; }

    public string Language { get; set; } = null!;

    public int? PublicationYear { get; set; }

    public int PublisherId { get; set; }

    public Publisher Publisher { get; set; } = null!;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];

    public ICollection<BookCategory> BookCategories { get; set; } = [];

    public ICollection<BookImage> BookImages { get; set; } = [];

    public ICollection<Listing> Listings { get; set; } = [];

    public ICollection<Review> Reviews { get; set; } = [];
}