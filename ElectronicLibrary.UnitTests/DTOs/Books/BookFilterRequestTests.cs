using System.ComponentModel.DataAnnotations;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.Enums;
using Xunit;

namespace ElectronicLibrary.UnitTests.DTOs.Books;

public class BookFilterRequestTests
{
    [Fact]
    public void Validate_WithDefaultValues_IsValid()
    {
        BookFilterRequest request = new();

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithValidValues_IsValid()
    {
        BookFilterRequest request = new()
        {
            SearchTerm = "Clean Code",
            PublisherId = 1,
            AuthorId = 2,
            CategoryId = 3,
            Language = "English",
            PublicationYear = 2020,
            MinPrice = 10m,
            MaxPrice = 100m,
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            InStock = true,
            SortBy = "price",
            SortDirection = "desc",
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
        BookFilterRequest request = new()
        {
            PageNumber = pageNumber
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validate_WithInvalidPageSize_ReturnsError(
        int pageSize)
    {
        BookFilterRequest request = new()
        {
            PageSize = pageSize
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.PageSize));
    }

    [Fact]
    public void Validate_WithNegativeMinPrice_ReturnsError()
    {
        BookFilterRequest request = new()
        {
            MinPrice = -1m
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.MinPrice));
    }

    [Fact]
    public void Validate_WithNegativeMaxPrice_ReturnsError()
    {
        BookFilterRequest request = new()
        {
            MaxPrice = -1m
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.MaxPrice));
    }

    [Fact]
    public void Validate_WhenMinPriceExceedsMaxPrice_ReturnsError()
    {
        BookFilterRequest request = new()
        {
            MinPrice = 100m,
            MaxPrice = 20m
        };

        IReadOnlyCollection<ValidationResult> errors =
            Validate(request);

        Assert.Contains(
            errors,
            error =>
                error.MemberNames.Contains(
                    nameof(BookFilterRequest.MinPrice)) &&
                error.MemberNames.Contains(
                    nameof(BookFilterRequest.MaxPrice)));
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public void Validate_WithInvalidFormat_ReturnsError(
        int format)
    {
        BookFilterRequest request = new()
        {
            Format = (BookFormat)format
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.Format));
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public void Validate_WithInvalidCondition_ReturnsError(
        int condition)
    {
        BookFilterRequest request = new()
        {
            Condition = (BookCondition)condition
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.Condition));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("author")]
    [InlineData("random")]
    public void Validate_WithInvalidSortBy_ReturnsError(
        string sortBy)
    {
        BookFilterRequest request = new()
        {
            SortBy = sortBy
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.SortBy));
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("random")]
    public void Validate_WithInvalidSortDirection_ReturnsError(
        string sortDirection)
    {
        BookFilterRequest request = new()
        {
            SortDirection = sortDirection
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.SortDirection));
    }

    [Fact]
    public void Validate_WithFuturePublicationYear_ReturnsError()
    {
        BookFilterRequest request = new()
        {
            PublicationYear =
                DateTime.UtcNow.Year + 1
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.PublicationYear));
    }

    [Fact]
    public void Validate_WithPublicationYearBelowMinimum_ReturnsError()
    {
        BookFilterRequest request = new()
        {
            PublicationYear = 999
        };

        AssertHasErrorFor(
            request,
            nameof(BookFilterRequest.PublicationYear));
    }

    private static void AssertHasErrorFor(
        BookFilterRequest request,
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

    private static IReadOnlyCollection<ValidationResult> Validate(
        BookFilterRequest request)
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