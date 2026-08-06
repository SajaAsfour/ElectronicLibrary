using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Carts;

public class UpdateCartItemQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
