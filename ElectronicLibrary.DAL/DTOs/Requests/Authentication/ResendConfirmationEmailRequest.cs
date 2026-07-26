using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class ResendConfirmationEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
