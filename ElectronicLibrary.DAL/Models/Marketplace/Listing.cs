using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.DAL.Models.Shopping;

namespace ElectronicLibrary.DAL.Models.Marketplace;

public class Listing
{
    public int ListingId { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public BookFormat Format { get; set; }

    public BookCondition? Condition { get; set; }

    public decimal DiscountPercentage { get; set; }

    public ListingStatus Status { get; set; }

    public int BookId { get; set; }

    public string SellerId { get; set; } = null!;

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedById { get; set; }

    public string? UpdatedById { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedById { get; set; }

    public Book Book { get; set; } = null!;

    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}