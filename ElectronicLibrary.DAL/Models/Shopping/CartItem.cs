using ElectronicLibrary.DAL.Models.Marketplace;

namespace ElectronicLibrary.DAL.Models.Shopping;

public class CartItem
{
    public int CartId { get; set; }

    public int ListingId { get; set; }

    public int Quantity { get; set; }

    public Cart Cart { get; set; } = null!;

    public Listing Listing { get; set; } = null!;
}