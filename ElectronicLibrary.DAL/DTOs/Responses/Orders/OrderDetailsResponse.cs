namespace ElectronicLibrary.DAL.DTOs.Responses.Orders;

public class OrderDetailsResponse
    : OrderSummaryResponse
{
    public string UserId { get; set; } =
        null!;

    public string? CouponDiscountType
    {
        get;
        set;
    }

    public decimal? CouponDiscountValue
    {
        get;
        set;
    }

    public List<OrderItemResponse> Items
    {
        get;
        set;
    } = [];
}