using ElectronicLibrary.BLL.Interfaces.Orders;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.PL.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy =
    AuthorizationPolicyNames.AdminOnly)]
public class AdminOrdersController
    : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(
            PagedResponse<
                OrderSummaryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
            PagedResponse<
                OrderSummaryResponse>>>
        GetAllOrders(
            [FromQuery]
            OrderFilterRequest request,
            CancellationToken cancellationToken)
    {
        PagedResponse<OrderSummaryResponse>
            response =
                await _orderService
                    .GetAllOrdersAsync(
                        request,
                        cancellationToken);

        return Ok(response);
    }

    [HttpGet("{orderId:int}")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
            OrderDetailsResponse>>
        GetOrderById(
            int orderId,
            CancellationToken cancellationToken)
    {
        OrderDetailsResponse response =
            await _orderService
                .GetOrderByIdForAdminAsync(
                    orderId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut(
        "order-items/{orderItemId:int}/status")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<
            OrderDetailsResponse>>
        UpdateOrderItemStatus(
            int orderItemId,
            [FromBody]
            UpdateOrderItemStatusRequest request,
            CancellationToken cancellationToken)
    {
        OrderDetailsResponse response =
            await _orderService
                .UpdateOrderItemStatusForAdminAsync(
                    orderItemId,
                    request,
                    cancellationToken);

        return Ok(response);
    }
}
