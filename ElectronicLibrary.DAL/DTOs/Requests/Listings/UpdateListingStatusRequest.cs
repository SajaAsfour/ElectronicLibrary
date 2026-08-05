using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Listings;

public class UpdateListingStatusRequest
{
    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus Status { get; set; }
}