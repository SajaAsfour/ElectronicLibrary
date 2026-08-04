using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Sellers;

public class UpdateSellerProfileRequest
{
    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = null!;

    [MaxLength(1000)]
    public string? SellerBio { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
