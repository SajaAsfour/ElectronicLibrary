using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Orders;

public class OrderConfiguration
    : IEntityTypeConfiguration<Order>
{
    public void Configure(
        EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order =>
            order.OrderId);

        builder.Property(order =>
                order.OrderDate)
            .HasDefaultValueSql(
                "SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(order =>
                order.SubtotalAmount)
            .HasPrecision(18, 2);

        builder.Property(order =>
                order.ListingDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(order =>
                order.CouponDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(order =>
                order.TotalDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(order =>
                order.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(order =>
                order.Status)
            .HasDefaultValue(
                OrderStatus.Pending)
            .IsRequired();

        builder.Property(order =>
                order.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(order =>
                order.CouponCodeSnapshot)
            .HasMaxLength(50);

        builder.Property(order =>
                order.CouponDiscountTypeSnapshot)
            .HasMaxLength(30);

        builder.Property(order =>
                order.CouponDiscountValueSnapshot)
            .HasPrecision(18, 2);

        builder.HasIndex(order =>
            new
            {
                order.UserId,
                order.OrderDate
            });

        builder.HasIndex(order =>
            order.Status);

        builder.HasOne(order =>
                order.User)
            .WithMany(user =>
                user.Orders)
            .HasForeignKey(order =>
                order.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order =>
                order.Coupon)
            .WithMany(coupon =>
                coupon.Orders)
            .HasForeignKey(order =>
                order.CouponId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}