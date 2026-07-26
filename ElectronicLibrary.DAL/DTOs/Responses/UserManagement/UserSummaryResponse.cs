namespace ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

public class UserSummaryResponse
{
    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? City { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsLockedOut { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public ICollection<string> Roles { get; set; } = [];
}
