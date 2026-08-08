namespace ElectronicLibrary.BLL.Helpers.Marketplace;

public static class ListingPriceCalculator
{
    public static decimal CalculateEffectiveUnitPrice(
        decimal unitPrice,
        decimal discountPercentage)
    {
        return unitPrice -
               unitPrice *
               discountPercentage /
               100m;
    }

    public static ListingPriceBreakdown
        CalculateLine(
            decimal unitPrice,
            decimal discountPercentage,
            int quantity)
    {
        decimal effectiveUnitPrice =
            CalculateEffectiveUnitPrice(
                unitPrice,
                discountPercentage);

        decimal lineSubtotal =
            unitPrice * quantity;

        decimal lineTotal =
            effectiveUnitPrice * quantity;

        decimal lineDiscount =
            lineSubtotal - lineTotal;

        return new ListingPriceBreakdown
        {
            UnitPrice = unitPrice,
            DiscountPercentage =
                discountPercentage,
            EffectiveUnitPrice =
                effectiveUnitPrice,
            Quantity = quantity,
            LineSubtotal = lineSubtotal,
            LineDiscount = lineDiscount,
            LineTotal = lineTotal
        };
    }
}

public sealed class ListingPriceBreakdown
{
    public decimal UnitPrice { get; init; }

    public decimal DiscountPercentage
    {
        get;
        init;
    }

    public decimal EffectiveUnitPrice
    {
        get;
        init;
    }

    public int Quantity { get; init; }

    public decimal LineSubtotal { get; init; }

    public decimal LineDiscount { get; init; }

    public decimal LineTotal { get; init; }
}