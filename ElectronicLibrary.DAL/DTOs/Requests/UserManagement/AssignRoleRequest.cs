using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.UserManagement;

public class AssignRoleRequest
{
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = null!;
}
