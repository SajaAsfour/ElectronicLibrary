using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Books;

public class UpdateBookRequest
{
    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string Title { get; set; } = null!;

    [StringLength(20)]
    public string? Isbn { get; set; }

    [StringLength(3000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Language { get; set; } = null!;

    [Range(1, 9999)]
    public int? PublicationYear { get; set; }

    [Range(1, int.MaxValue)]
    public int PublisherId { get; set; }

    [Required]
    [MinLength(1)]
    public ICollection<int> AuthorIds { get; set; } = [];

    [Required]
    [MinLength(1)]
    public ICollection<int> CategoryIds { get; set; } = [];
}