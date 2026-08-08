using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Discounts;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Payments;

namespace ElectronicLibrary.DAL.Models.Orders;

public class Order
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

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

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string UserId { get; set; } = null!;

    public int? CouponId { get; set; }

    public string? CouponCodeSnapshot
    {
        get;
        set;
    }

    public string? CouponDiscountTypeSnapshot
    {
        get;
        set;
    }

    public decimal? CouponDiscountValueSnapshot
    {
        get;
        set;
    }

    public ApplicationUser User { get; set; } =
        null!;

    public Coupon? Coupon { get; set; }

    public Payment? Payment { get; set; }

    public ICollection<OrderItem> OrderItems
    {
        get;
        set;
    } = [];
}
