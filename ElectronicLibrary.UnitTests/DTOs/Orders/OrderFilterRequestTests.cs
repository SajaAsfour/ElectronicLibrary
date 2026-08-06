using System.ComponentModel.DataAnnotations;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.Enums;

namespace ElectronicLibrary.UnitTests.DTOs.Orders;

public class OrderFilterRequestTests
{
    [Fact]
    public void Validate_WithDefaultValues_IsValid()
    {
        OrderFilterRequest request = new();

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);

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
        OrderFilterRequest request =
            new()
            {
                Status =
                    OrderStatus.Processing,
                FromDate =
                    DateTime.UtcNow.AddDays(-7),
                ToDate =
                    DateTime.UtcNow,
                SortBy =
                    "totalAmount",
                SortDirection =
                    "asc",
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
    public void Validate_WithInvalidPageNumber_ReturnsError(
        int pageNumber)
    {
        OrderFilterRequest request =
            new()
            {
                PageNumber =
                    pageNumber
            };

        AssertHasErrorFor(
            request,
            nameof(
                OrderFilterRequest.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validate_WithInvalidPageSize_ReturnsError(
        int pageSize)
    {
        OrderFilterRequest request =
            new()
            {
                PageSize =
                    pageSize
            };

        AssertHasErrorFor(
            request,
            nameof(
                OrderFilterRequest.PageSize));
    }

    [Theory]
    [InlineData("orderDate")]
    [InlineData("date")]
    [InlineData("totalAmount")]
    [InlineData("total")]
    [InlineData("status")]
    [InlineData("ORDERDATE")]
    [InlineData("TotalAmount")]
    public void Validate_WithSupportedSortBy_IsValid(
        string sortBy)
    {
        OrderFilterRequest request =
            new()
            {
                SortBy =
                    sortBy
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.DoesNotContain(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        OrderFilterRequest.SortBy)));
    }

    [Theory]
    [InlineData("price")]
    [InlineData("customer")]
    [InlineData("createdAt")]
    [InlineData("unknown")]
    public void Validate_WithInvalidSortBy_ReturnsError(
        string sortBy)
    {
        OrderFilterRequest request =
            new()
            {
                SortBy =
                    sortBy
            };

        AssertHasErrorFor(
            request,
            nameof(
                OrderFilterRequest.SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    [InlineData("ASC")]
    [InlineData("DESC")]
    public void Validate_WithSupportedSortDirection_IsValid(
        string sortDirection)
    {
        OrderFilterRequest request =
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
                        OrderFilterRequest
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
        OrderFilterRequest request =
            new()
            {
                SortDirection =
                    sortDirection
            };

        AssertHasErrorFor(
            request,
            nameof(
                OrderFilterRequest.SortDirection));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public void Validate_WithInvalidStatus_ReturnsError(
        int status)
    {
        OrderFilterRequest request =
            new()
            {
                Status =
                    (OrderStatus)status
            };

        AssertHasErrorFor(
            request,
            nameof(
                OrderFilterRequest.Status));
    }

    [Fact]
    public void Validate_WhenFromDateIsLaterThanToDate_ReturnsErrorForBothDates()
    {
        OrderFilterRequest request =
            new()
            {
                FromDate =
                    DateTime.UtcNow,
                ToDate =
                    DateTime.UtcNow
                        .AddDays(-1)
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Contains(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(
                        OrderFilterRequest
                            .FromDate)) &&
                error.MemberNames.Contains(
                    nameof(
                        OrderFilterRequest
                            .ToDate)) &&
                error.ErrorMessage ==
                    "FromDate cannot be later than ToDate.");
    }

    [Fact]
    public void Validate_WhenFromDateEqualsToDate_IsValid()
    {
        DateTime date =
            DateTime.UtcNow.Date;

        OrderFilterRequest request =
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
        OrderFilterRequest request =
            new()
            {
                FromDate =
                    DateTime.UtcNow
                        .AddDays(-5)
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithOnlyToDate_IsValid()
    {
        OrderFilterRequest request =
            new()
            {
                ToDate =
                    DateTime.UtcNow
            };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    private static void AssertHasErrorFor(
        OrderFilterRequest request,
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
            OrderFilterRequest request)
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
