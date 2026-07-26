using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class UpdateAddressRequest
{
    [Required(AllowEmptyStrings = true)]
    [MaxLength(500)]
    public string Address { get; set; } = null!;
}