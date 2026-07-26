using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class DeleteAccountRequest
{
    [Required]
    public string CurrentPassword { get; set; } = null!;
}