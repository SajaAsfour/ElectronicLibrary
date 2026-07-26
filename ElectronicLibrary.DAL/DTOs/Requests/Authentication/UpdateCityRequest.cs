using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Authentication;

public class UpdateCityRequest
{
    [Required(AllowEmptyStrings = true)]
    [MaxLength(100)]
    public string City { get; set; } = null!;
}