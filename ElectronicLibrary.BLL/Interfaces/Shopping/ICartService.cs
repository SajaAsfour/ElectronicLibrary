using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using ElectronicLibrary.DAL.DTOs.Responses.Carts;

namespace ElectronicLibrary.BLL.Interfaces.Shopping;

public interface ICartService
{
    Task<CartResponse> GetCurrentUserCartAsync(
        CancellationToken cancellationToken = default);

    Task<CartResponse> AddCartItemAsync(
        AddCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartResponse> UpdateCartItemQuantityAsync(
        int listingId,
        UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveCartItemAsync(
        int listingId,
        CancellationToken cancellationToken = default);

    Task ClearCartAsync(
        CancellationToken cancellationToken = default);
}
