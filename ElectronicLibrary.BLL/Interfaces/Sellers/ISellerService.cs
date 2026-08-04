using ElectronicLibrary.DAL.DTOs.Requests.Sellers;
using ElectronicLibrary.DAL.DTOs.Responses.Sellers;

namespace ElectronicLibrary.BLL.Interfaces.Sellers;

public interface ISellerService
{
    Task<SellerProfileResponse> ActivateSellerAsync(
        ActivateSellerRequest request,
        CancellationToken cancellationToken = default);

    Task<SellerProfileResponse> GetCurrentSellerProfileAsync(
        CancellationToken cancellationToken = default);

    Task<SellerProfileResponse> UpdateCurrentSellerProfileAsync(
        UpdateSellerProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<PublicSellerProfileResponse> GetPublicSellerProfileAsync(
        string sellerId,
        CancellationToken cancellationToken = default);
}
