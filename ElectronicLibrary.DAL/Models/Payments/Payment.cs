using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Orders;

namespace ElectronicLibrary.DAL.Models.Payments;

public class Payment
{
    public int PaymentId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;
}