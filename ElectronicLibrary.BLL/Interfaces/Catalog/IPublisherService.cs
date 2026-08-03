using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Publishers;

namespace ElectronicLibrary.BLL.Interfaces.Catalog;

public interface IPublisherService
{
    Task<PagedResponse<PublisherResponse>> GetPublishersAsync(
        PublisherQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<PublisherDetailsResponse> GetPublisherByIdAsync(
        int publisherId,
        CancellationToken cancellationToken = default);

    Task<PublisherResponse> CreatePublisherAsync(
        CreatePublisherRequest request,
        CancellationToken cancellationToken = default);

    Task<PublisherResponse> UpdatePublisherAsync(
        int publisherId,
        UpdatePublisherRequest request,
        CancellationToken cancellationToken = default);

    Task DeletePublisherAsync(
        int publisherId,
        CancellationToken cancellationToken = default);
}