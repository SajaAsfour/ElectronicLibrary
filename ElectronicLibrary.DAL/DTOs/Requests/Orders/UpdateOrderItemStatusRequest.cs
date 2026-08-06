using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.DAL.DTOs.Requests.Orders;

public class UpdateOrderItemStatusRequest
{
    public OrderItemStatus Status { get; set; }
}