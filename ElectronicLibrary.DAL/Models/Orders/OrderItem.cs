using ElectronicLibrary.DAL.Models.Marketplace;

namespace ElectronicLibrary.DAL.Models.Orders;

public class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ListingId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public Order Order { get; set; } = null!;

    public Listing Listing { get; set; } = null!;
}