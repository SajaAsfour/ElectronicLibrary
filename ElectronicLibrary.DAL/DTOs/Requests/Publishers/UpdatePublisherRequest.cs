using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Publishers;

public class UpdatePublisherRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    [Url]
    public string? Website { get; set; }
}