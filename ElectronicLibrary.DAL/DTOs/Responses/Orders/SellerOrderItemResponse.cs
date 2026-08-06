namespace ElectronicLibrary.DAL.DTOs.Responses.Orders;

public class SellerOrderItemResponse
    : OrderItemResponse
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }
}