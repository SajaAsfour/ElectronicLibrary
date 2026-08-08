using ElectronicLibrary.BLL.Interfaces.Orders;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.PL.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/seller/order-items")]
[Authorize(Policy =
    AuthorizationPolicyNames.SellerOnly)]
public class SellerOrderItemsController
    : ControllerBase
{
    private readonly IOrderService _orderService;

    public SellerOrderItemsController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(
            PagedResponse<
                SellerOrderItemResponse>),
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
            PagedResponse<
                SellerOrderItemResponse>>>
        GetCurrentSellerOrderItems(
            [FromQuery]
            SellerOrderItemFilterRequest request,
            CancellationToken cancellationToken)
    {
        PagedResponse<SellerOrderItemResponse>
            response =
                await _orderService
                    .GetCurrentSellerOrderItemsAsync(
                        request,
                        cancellationToken);

        return Ok(response);
    }

    [HttpPut("{orderItemId:int}/status")]
    [ProducesResponseType(
        typeof(SellerOrderItemResponse),
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
            SellerOrderItemResponse>>
        UpdateCurrentSellerOrderItemStatus(
            int orderItemId,
            [FromBody]
            UpdateOrderItemStatusRequest request,
            CancellationToken cancellationToken)
    {
        SellerOrderItemResponse response =
            await _orderService
                .UpdateCurrentSellerOrderItemStatusAsync(
                    orderItemId,
                    request,
                    cancellationToken);

        return Ok(response);
    }
}
