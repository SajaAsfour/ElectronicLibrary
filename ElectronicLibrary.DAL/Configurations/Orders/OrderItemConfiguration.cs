using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Orders;

public class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(
        EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(orderItem =>
            orderItem.OrderItemId);

        builder.Property(orderItem =>
                orderItem.SellerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(orderItem =>
                orderItem.BookTitleSnapshot)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(orderItem =>
                orderItem.SellerStoreNameSnapshot)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(orderItem =>
                orderItem.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(orderItem =>
                orderItem.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(orderItem =>
                orderItem.EffectiveUnitPrice)
            .HasPrecision(18, 2);

        builder.Property(orderItem =>
                orderItem.LineSubtotal)
            .HasPrecision(18, 2);

        builder.Property(orderItem =>
                orderItem.LineDiscount)
            .HasPrecision(18, 2);

        builder.Property(orderItem =>
                orderItem.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(orderItem =>
                orderItem.Status)
            .HasDefaultValue(
                OrderItemStatus.Pending)
            .IsRequired();

        builder.HasIndex(orderItem =>
            orderItem.OrderId);

        builder.HasIndex(orderItem =>
            orderItem.ListingId);

        builder.HasIndex(orderItem =>
            new
            {
                orderItem.SellerId,
                orderItem.Status
            });

        builder.HasOne(orderItem =>
                orderItem.Order)
            .WithMany(order =>
                order.OrderItems)
            .HasForeignKey(orderItem =>
                orderItem.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(orderItem =>
                orderItem.Listing)
            .WithMany(listing =>
                listing.OrderItems)
            .HasForeignKey(orderItem =>
                orderItem.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}