using ElectronicLibrary.BLL.Interfaces.Orders;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsResponse>>
        Checkout(
            [FromBody] CheckoutRequest request,
            CancellationToken cancellationToken)
    {
        OrderDetailsResponse response =
            await _orderService.CheckoutAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetCurrentUserOrderById),
            new
            {
                orderId = response.OrderId
            },
            response);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<OrderSummaryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<
            PagedResponse<OrderSummaryResponse>>>
        GetCurrentUserOrders(
            [FromQuery] OrderFilterRequest request,
            CancellationToken cancellationToken)
    {
        PagedResponse<OrderSummaryResponse>
            response =
                await _orderService
                    .GetCurrentUserOrdersAsync(
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
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsResponse>>
        GetCurrentUserOrderById(
            int orderId,
            CancellationToken cancellationToken)
    {
        OrderDetailsResponse response =
            await _orderService
                .GetCurrentUserOrderByIdAsync(
                    orderId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPost("{orderId:int}/cancel")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsResponse>>
        CancelCurrentUserOrder(
            int orderId,
            CancellationToken cancellationToken)
    {
        OrderDetailsResponse response =
            await _orderService
                .CancelCurrentUserOrderAsync(
                    orderId,
                    cancellationToken);

        return Ok(response);
    }
}
