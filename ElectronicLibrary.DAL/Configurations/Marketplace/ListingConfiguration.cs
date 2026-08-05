using ElectronicLibrary.DAL.Models.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Marketplace;

public class ListingConfiguration
    : IEntityTypeConfiguration<Listing>
{
    public void Configure(
        EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");

        builder.HasKey(listing =>
            listing.ListingId);

        builder.Property(listing =>
                listing.Price)
            .HasPrecision(18, 2);

        builder.Property(listing =>
                listing.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(listing =>
                listing.SellerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(listing =>
                listing.CreatedAt)
            .HasDefaultValueSql(
                "SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(listing =>
                listing.CreatedById)
            .HasMaxLength(450);

        builder.Property(listing =>
                listing.UpdatedById)
            .HasMaxLength(450);

        builder.Property(listing =>
                listing.DeletedById)
            .HasMaxLength(450);

        builder.Property(listing =>
                listing.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(listing =>
                listing.Book)
            .WithMany(book =>
                book.Listings)
            .HasForeignKey(listing =>
                listing.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(listing =>
                listing.Seller)
            .WithMany(seller =>
                seller.Listings)
            .HasForeignKey(listing =>
                listing.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(listing =>
            !listing.IsDeleted &&
            !listing.Book.IsDeleted);
    }
}