using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Listings;

public class BookListingFilterRequest
{
    private const int MaximumPageSize = 50;

    [EnumDataType(typeof(BookFormat))]
    public BookFormat? Format { get; set; }

    [EnumDataType(typeof(BookCondition))]
    public BookCondition? Condition { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; set; } = 10;
}