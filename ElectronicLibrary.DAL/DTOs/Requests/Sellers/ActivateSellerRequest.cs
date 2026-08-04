using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Sellers;

public class ActivateSellerRequest
{
    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = null!;

    [MaxLength(1000)]
    public string? SellerBio { get; set; }
}