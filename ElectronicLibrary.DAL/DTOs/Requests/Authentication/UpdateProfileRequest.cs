using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = null!;
}