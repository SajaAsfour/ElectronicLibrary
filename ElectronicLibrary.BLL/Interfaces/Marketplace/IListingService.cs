using ElectronicLibrary.DAL.DTOs.Requests.Listings;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Listings;

namespace ElectronicLibrary.BLL.Interfaces.Marketplace;

public interface IListingService
{
    Task<ListingResponse> CreateListingAsync(
        CreateListingRequest request,
        CancellationToken cancellationToken = default);

    Task<ListingResponse> GetListingByIdAsync(
        int listingId,
        CancellationToken cancellationToken = default);

    Task<ListingResponse> UpdateListingAsync(
        int listingId,
        UpdateListingRequest request,
        CancellationToken cancellationToken = default);

    Task<ListingResponse> UpdateListingStatusAsync(
        int listingId,
        UpdateListingStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteListingAsync(
        int listingId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ListingResponse>>
        GetCurrentSellerListingsAsync(
            SellerListingFilterRequest request,
            CancellationToken cancellationToken = default);

    Task<PagedResponse<ListingResponse>>
        GetBookListingsAsync(
            int bookId,
            BookListingFilterRequest request,
            CancellationToken cancellationToken = default);
}