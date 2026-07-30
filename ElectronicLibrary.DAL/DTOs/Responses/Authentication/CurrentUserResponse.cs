namespace ElectronicLibrary.DAL.DTOs.Responses.Authentication;

public class CurrentUserResponse
{
    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? StoreName { get; set; }

    public string? SellerBio { get; set; }

    public decimal? SellerRating { get; set; }

    public bool EmailConfirmed { get; set; }

    public ICollection<string> Roles { get; set; } = [];
}