using ElectronicLibrary.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.DAL.DTOs.Requests.Orders;

public class OrderFilterRequest
    : IValidatableObject
{
    private const int MaximumPageSize = 50;

    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    [RegularExpression(
        "(?i)^(orderdate|date|totalamount|total|status)$",
        ErrorMessage =
            "SortBy must be orderDate, totalAmount, or status.")]
    public string SortBy { get; set; } =
        "orderDate";

    [RegularExpression(
        "(?i)^(asc|desc)$",
        ErrorMessage =
            "SortDirection must be asc or desc.")]
    public string SortDirection { get; set; } =
        "desc";

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Status.HasValue &&
            !Enum.IsDefined(Status.Value))
        {
            yield return new ValidationResult(
                "The selected order status is invalid.",
                [nameof(Status)]);
        }

        if (FromDate.HasValue &&
            ToDate.HasValue &&
            FromDate.Value > ToDate.Value)
        {
            yield return new ValidationResult(
                "FromDate cannot be later than ToDate.",
                [
                    nameof(FromDate),
                    nameof(ToDate)
                ]);
        }
    }
}