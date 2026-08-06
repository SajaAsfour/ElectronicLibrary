using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Carts;

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ListingId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}