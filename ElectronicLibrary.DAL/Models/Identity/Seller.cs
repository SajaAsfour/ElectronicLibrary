using ElectronicLibrary.DAL.Models.Identity;

namespace ElectronicLibrary.DAL.Models.Marketplace;

public class Seller
{
    public int SellerId { get; set; }

    public string UserId { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public decimal Rating { get; set; }

    public string? Bio { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<Listing> Listings { get; set; } = [];
}