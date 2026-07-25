using ElectronicLibrary.DAL.Models.Orders;

namespace ElectronicLibrary.DAL.Models.Discounts;

public class Coupon
{
    public int CouponId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal DiscountValue { get; set; }

    public string DiscountType { get; set; } = null!;

    public bool IsActive { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}