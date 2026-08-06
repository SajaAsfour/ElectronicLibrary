using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.DAL.DTOs.Responses.Orders;

public class OrderSummaryResponse
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public int TotalItems { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal ListingDiscountAmount
    {
        get;
        set;
    }

    public decimal CouponDiscountAmount
    {
        get;
        set;
    }

    public decimal TotalDiscountAmount
    {
        get;
        set;
    }

    public decimal TotalAmount { get; set; }

    public string? CouponCode { get; set; }
}