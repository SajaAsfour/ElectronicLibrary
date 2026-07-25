using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;

namespace ElectronicLibrary.DAL.Models.Reviews;

public class Review
{
    public int ReviewId { get; set; }

    public string UserId { get; set; } = null!;

    public int Rating { get; set; }

    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

    public string? Comment { get; set; }

    public int BookId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Book Book { get; set; } = null!;
}