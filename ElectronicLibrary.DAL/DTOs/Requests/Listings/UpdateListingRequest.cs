using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Listings;

public class UpdateListingRequest
{
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [EnumDataType(typeof(BookFormat))]
    public BookFormat Format { get; set; }

    [EnumDataType(typeof(BookCondition))]
    public BookCondition? Condition { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "100")]
    public decimal DiscountPercentage { get; set; }
}