using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class UpdateSellerProfileRequest
{
    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = null!;

    [MaxLength(1000)]
    public string? Bio { get; set; }
}
