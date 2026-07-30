namespace ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

public class UserDetailsResponse : UserSummaryResponse
{
    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? StoreName { get; set; }

    public string? SellerBio { get; set; }

    public decimal? SellerRating { get; set; }
}
