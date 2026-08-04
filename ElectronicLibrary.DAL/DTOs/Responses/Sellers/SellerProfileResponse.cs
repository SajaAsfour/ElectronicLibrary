namespace ElectronicLibrary.DAL.DTOs.Responses.Sellers;

public class SellerProfileResponse
{
    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public string? SellerBio { get; set; }

    public decimal? SellerRating { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public bool IsSeller { get; set; }
}