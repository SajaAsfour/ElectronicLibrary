namespace ElectronicLibrary.DAL.DTOs.Responses.Sellers;

public class PublicSellerProfileResponse
{
    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public string? SellerBio { get; set; }

    public decimal? SellerRating { get; set; }

    public string? City { get; set; }
}