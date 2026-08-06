using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;

namespace ElectronicLibrary.BLL.Interfaces.Orders;

public interface IOrderService
{
    Task<OrderDetailsResponse> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<OrderSummaryResponse>>
        GetCurrentUserOrdersAsync(
            OrderFilterRequest request,
            CancellationToken cancellationToken = default);

    Task<OrderDetailsResponse>
        GetCurrentUserOrderByIdAsync(
            int orderId,
            CancellationToken cancellationToken = default);

    Task<OrderDetailsResponse>
        CancelCurrentUserOrderAsync(
            int orderId,
            CancellationToken cancellationToken = default);

    Task<PagedResponse<SellerOrderItemResponse>>
        GetCurrentSellerOrderItemsAsync(
            SellerOrderItemFilterRequest request,
            CancellationToken cancellationToken = default);

    Task<SellerOrderItemResponse>
        UpdateCurrentSellerOrderItemStatusAsync(
            int orderItemId,
            UpdateOrderItemStatusRequest request,
            CancellationToken cancellationToken = default);

    Task<PagedResponse<OrderSummaryResponse>>
        GetAllOrdersAsync(
            OrderFilterRequest request,
            CancellationToken cancellationToken = default);

    Task<OrderDetailsResponse>
        GetOrderByIdForAdminAsync(
            int orderId,
            CancellationToken cancellationToken = default);

    Task<OrderDetailsResponse>
        UpdateOrderItemStatusForAdminAsync(
            int orderItemId,
            UpdateOrderItemStatusRequest request,
            CancellationToken cancellationToken = default);
}