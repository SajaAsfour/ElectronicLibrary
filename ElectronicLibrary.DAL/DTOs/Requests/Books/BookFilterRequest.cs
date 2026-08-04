using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Books;

public class BookFilterRequest : IValidatableObject
{
    [StringLength(200)]
    public string? SearchTerm { get; set; }

    [Range(1, int.MaxValue)]
    public int? PublisherId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AuthorId { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? Language { get; set; }

    public int? PublicationYear { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    [EnumDataType(typeof(BookFormat))]
    public BookFormat? Format { get; set; }

    [EnumDataType(typeof(BookCondition))]
    public BookCondition? Condition { get; set; }

    public bool? InStock { get; set; }

    [RegularExpression(
        "(?i)^(title|publicationyear|year|price|lowestprice|availablelistingscount|listingscount)$",
        ErrorMessage =
            "SortBy must be title, publicationYear, price, or availableListingsCount.")]
    public string SortBy { get; set; } = "title";

    [RegularExpression(
        "(?i)^(asc|desc)$",
        ErrorMessage =
            "SortDirection must be asc or desc.")]
    public string SortDirection { get; set; } = "asc";

    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "PageNumber must be greater than or equal to 1.")]
    public int PageNumber { get; set; } = 1;

    [Range(
        1,
        50,
        ErrorMessage =
            "PageSize must be between 1 and 50.")]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (PublicationYear.HasValue &&
            (PublicationYear.Value < 1000 ||
             PublicationYear.Value > DateTime.UtcNow.Year))
        {
            yield return new ValidationResult(
                $"PublicationYear must be between 1000 and {DateTime.UtcNow.Year}.",
                [nameof(PublicationYear)]);
        }

        if (MinPrice.HasValue &&
            MinPrice.Value < 0)
        {
            yield return new ValidationResult(
                "MinPrice cannot be negative.",
                [nameof(MinPrice)]);
        }

        if (MaxPrice.HasValue &&
            MaxPrice.Value < 0)
        {
            yield return new ValidationResult(
                "MaxPrice cannot be negative.",
                [nameof(MaxPrice)]);
        }

        if (MinPrice.HasValue &&
            MaxPrice.HasValue &&
            MinPrice.Value > MaxPrice.Value)
        {
            yield return new ValidationResult(
                "MinPrice cannot be greater than MaxPrice.",
                [
                    nameof(MinPrice),
                    nameof(MaxPrice)
                ]);
        }

        if (Format.HasValue &&
            !Enum.IsDefined(Format.Value))
        {
            yield return new ValidationResult(
                "The selected book format is invalid.",
                [nameof(Format)]);
        }

        if (Condition.HasValue &&
            !Enum.IsDefined(Condition.Value))
        {
            yield return new ValidationResult(
                "The selected book condition is invalid.",
                [nameof(Condition)]);
        }
    }
}