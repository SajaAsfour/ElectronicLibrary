using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Listings;

public class SellerListingFilterRequest
{
    private const int MaximumPageSize = 50;

    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; set; } = 10;
}