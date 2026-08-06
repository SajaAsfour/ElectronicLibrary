using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.DAL.DTOs.Responses.Carts;

public class CartItemResponse
{
    public int ListingId { get; set; }

    public int BookId { get; set; }

    public string BookTitle { get; set; } = null!;

    public string? MainImageUrl { get; set; }

    public string SellerId { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public int Quantity { get; set; }

    public int AvailableQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal EffectiveUnitPrice { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineDiscount { get; set; }

    public decimal LineTotal { get; set; }

    public BookFormat Format { get; set; }

    public BookCondition? Condition { get; set; }

    public ListingStatus ListingStatus { get; set; }

    public bool IsAvailable { get; set; }

    public string? AvailabilityMessage { get; set; }
}
