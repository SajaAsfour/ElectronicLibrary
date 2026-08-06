namespace ElectronicLibrary.DAL.DTOs.Responses.Carts;

public class CartResponse
{
    public int CartId { get; set; }

    public string UserId { get; set; } = null!;

    public IReadOnlyCollection<CartItemResponse> Items { get; set; }
        = Array.Empty<CartItemResponse>();

    public int TotalItems { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal FinalTotal { get; set; }
}
