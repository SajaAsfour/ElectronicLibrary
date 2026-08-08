using System.ComponentModel.DataAnnotations;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.UnitTests.DTOs.Orders;

public class SellerOrderItemFilterRequestTests
{
    [Fact]
    public void Validate_WithDefaultValues_IsValid()
    {
        SellerOrderItemFilterRequest request =
            new();

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);

        Assert.Null(request.OrderId);
        Assert.Null(request.Status);

        Assert.Equal(
            "orderDate",
            request.SortBy);

        Assert.Equal(
            "desc",
            request.SortDirection);

        Assert.Equal(
            1,
            request.PageNumber);

        Assert.Equal(
            10,
            request.PageSize);
    }

    [Fact]
    public void Validate_WithValidValues_IsValid()
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                OrderId = 10,
                Status =
                    OrderItemStatus.Processing,
                FromDate =
                    DateTime.UtcNow.AddDays(-7),
                ToDate =
                    DateTime.UtcNow,
                SortBy = "lineTotal",
                SortDirection = "asc",
                PageNumber = 2,
                PageSize = 20
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidOrderId_ReturnsError(
        int orderId)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                OrderId = orderId
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .OrderId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_WithValidOrderId_IsValid(
        int orderId)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                OrderId = orderId
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.DoesNotContain(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .OrderId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPageNumber_ReturnsError(
        int pageNumber)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                PageNumber = pageNumber
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validate_WithInvalidPageSize_ReturnsError(
        int pageSize)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                PageSize = pageSize
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .PageSize));
    }

    [Theory]
    [InlineData("orderDate")]
    [InlineData("date")]
    [InlineData("lineTotal")]
    [InlineData("total")]
    [InlineData("status")]
    [InlineData("ORDERDATE")]
    [InlineData("LineTotal")]
    public void Validate_WithSupportedSortBy_IsValid(
        string sortBy)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                SortBy = sortBy
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.DoesNotContain(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .SortBy)));
    }

    [Theory]
    [InlineData("price")]
    [InlineData("book")]
    [InlineData("seller")]
    [InlineData("unknown")]
    public void Validate_WithInvalidSortBy_ReturnsError(
        string sortBy)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                SortBy = sortBy
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    [InlineData("ASC")]
    [InlineData("DESC")]
    public void Validate_WithSupportedSortDirection_IsValid(
        string sortDirection)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                SortDirection =
                    sortDirection
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.DoesNotContain(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .SortDirection)));
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("newest")]
    [InlineData("random")]
    public void Validate_WithInvalidSortDirection_ReturnsError(
        string sortDirection)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                SortDirection =
                    sortDirection
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .SortDirection));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public void Validate_WithInvalidStatus_ReturnsError(
        int status)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                Status =
                    (OrderItemStatus)status
            };

        AssertHasErrorFor(
            request,
            nameof(
                SellerOrderItemFilterRequest
                    .Status));
    }

    [Theory]
    [InlineData(OrderItemStatus.Pending)]
    [InlineData(OrderItemStatus.Confirmed)]
    [InlineData(OrderItemStatus.Processing)]
    [InlineData(OrderItemStatus.Shipped)]
    [InlineData(OrderItemStatus.Delivered)]
    [InlineData(OrderItemStatus.Cancelled)]
    public void Validate_WithDefinedStatus_IsValid(
        OrderItemStatus status)
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                Status = status
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.DoesNotContain(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .Status)));
    }

    [Fact]
    public void Validate_WhenFromDateIsLaterThanToDate_ReturnsErrorForBothDates()
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                FromDate =
                    DateTime.UtcNow,
                ToDate =
                    DateTime.UtcNow.AddDays(-1)
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Contains(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .FromDate)) &&
                error.MemberNames.Contains(
                    nameof(
                        SellerOrderItemFilterRequest
                            .ToDate)) &&
                error.ErrorMessage ==
                    "FromDate cannot be later than ToDate.");
    }

    [Fact]
    public void Validate_WhenFromDateEqualsToDate_IsValid()
    {
        DateTime date =
            DateTime.UtcNow.Date;

        SellerOrderItemFilterRequest request =
            new()
            {
                FromDate = date,
                ToDate = date
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithOnlyFromDate_IsValid()
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                FromDate =
                    DateTime.UtcNow.AddDays(-5)
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithOnlyToDate_IsValid()
    {
        SellerOrderItemFilterRequest request =
            new()
            {
                ToDate = DateTime.UtcNow
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    private static void AssertHasErrorFor(
        SellerOrderItemFilterRequest request,
        string propertyName)
    {
        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Contains(
            errors,
            error =>
                error.MemberNames.Contains(
                    propertyName));
    }

    private static IReadOnlyCollection<ValidationResult>
        Validate(
            SellerOrderItemFilterRequest request)
    {
        List<ValidationResult> results = [];

        ValidationContext validationContext =
            new(request);

        Validator.TryValidateObject(
            request,
            validationContext,
            results,
            validateAllProperties: true);

        return results;
    }
}
