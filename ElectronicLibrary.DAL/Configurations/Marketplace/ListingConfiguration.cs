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

        builder.HasKey(x => x.ListingId);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(x => x.SellerId)
            .IsRequired();

        builder.HasOne(x => x.Book)
            .WithMany(x => x.Listings)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Listings)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}