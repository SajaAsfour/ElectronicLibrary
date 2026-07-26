namespace ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

public class UserDetailsResponse : UserSummaryResponse
{
    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }
}
