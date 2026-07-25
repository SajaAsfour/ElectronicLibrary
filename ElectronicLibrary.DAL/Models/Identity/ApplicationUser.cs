using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.DAL.Models.Reviews;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ElectronicLibrary.DAL.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Seller? SellerProfile { get; set; }

    public Cart? Cart { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<Review> Reviews { get; set; } = [];
}