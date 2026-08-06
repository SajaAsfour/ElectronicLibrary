using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using System.ComponentModel.DataAnnotations;

namespace ElectronicLibrary.UnitTests.DTOs.Carts;

public class CartRequestTests
{
    [Fact]
    public void AddCartItemRequest_WhenValuesAreValid_PassesValidation()
    {
        var request = new AddCartItemRequest
        {
            ListingId = 1,
            Quantity = 2
        };

        List<ValidationResult> results =
            Validate(request);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCartItemRequest_WhenListingIdIsInvalid_FailsValidation(
        int listingId)
    {
        var request = new AddCartItemRequest
        {
            ListingId = listingId,
            Quantity = 1
        };

        List<ValidationResult> results =
            Validate(request);

        Assert.Contains(
            results,
            result =>
                result.MemberNames.Contains(
                    nameof(
                        AddCartItemRequest
                            .ListingId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCartItemRequest_WhenQuantityIsInvalid_FailsValidation(
        int quantity)
    {
        var request = new AddCartItemRequest
        {
            ListingId = 1,
            Quantity = quantity
        };

        List<ValidationResult> results =
            Validate(request);

        Assert.Contains(
            results,
            result =>
                result.MemberNames.Contains(
                    nameof(
                        AddCartItemRequest
                            .Quantity)));
    }

    [Fact]
    public void UpdateCartItemQuantityRequest_WhenQuantityIsValid_PassesValidation()
    {
        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 3
            };

        List<ValidationResult> results =
            Validate(request);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateCartItemQuantityRequest_WhenQuantityIsInvalid_FailsValidation(
        int quantity)
    {
        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = quantity
            };

        List<ValidationResult> results =
            Validate(request);

        Assert.Contains(
            results,
            result =>
                result.MemberNames.Contains(
                    nameof(
                        UpdateCartItemQuantityRequest
                            .Quantity)));
    }

    private static List<ValidationResult>
        Validate(object model)
    {
        var results =
            new List<ValidationResult>();

        var validationContext =
            new ValidationContext(model);

        Validator.TryValidateObject(
            model,
            validationContext,
            results,
            validateAllProperties: true);

        return results;
    }
}
