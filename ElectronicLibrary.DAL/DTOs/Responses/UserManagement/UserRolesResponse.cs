namespace ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

public class UserRolesResponse
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public ICollection<string> Roles { get; set; } = [];
}
