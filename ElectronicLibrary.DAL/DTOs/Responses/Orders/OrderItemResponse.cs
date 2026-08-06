using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.DAL.DTOs.Responses.Orders;

public class OrderItemResponse
{
    public int OrderItemId { get; set; }

    public int ListingId { get; set; }

    public int BookId { get; set; }

    public string SellerId { get; set; } =
        null!;

    public string BookTitle { get; set; } =
        null!;

    public string SellerStoreName { get; set; } =
        null!;

    public BookFormat Format { get; set; }

    public BookCondition? Condition { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage
    {
        get;
        set;
    }

    public decimal EffectiveUnitPrice
    {
        get;
        set;
    }

    public decimal LineSubtotal { get; set; }

    public decimal LineDiscount { get; set; }

    public decimal LineTotal { get; set; }

    public OrderItemStatus Status { get; set; }
}