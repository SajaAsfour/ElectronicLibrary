namespace ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

public class UserRoleUpdateResponse
{
    public string Message { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public ICollection<string> Roles { get; set; } = [];
}
