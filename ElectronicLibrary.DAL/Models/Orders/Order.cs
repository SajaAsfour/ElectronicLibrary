using ElectronicLibrary.DAL.Models.Discounts;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Payments;

namespace ElectronicLibrary.DAL.Models.Orders;

public class Order
{
    public int OrderId { get; set; }

    public int? CouponId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public string UserId { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public Coupon? Coupon { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public Payment? Payment { get; set; }
}