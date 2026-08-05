using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.DAL.DTOs.Responses.Listings;

public class ListingResponse
{
    public int ListingId { get; set; }

    public int BookId { get; set; }

    public string BookTitle { get; set; } = null!;

    public string SellerId { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal EffectivePrice { get; set; }

    public int Quantity { get; set; }

    public BookFormat Format { get; set; }

    public BookCondition? Condition { get; set; }

    public ListingStatus Status { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}