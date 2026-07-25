using ElectronicLibrary.DAL.Models.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Marketplace;

public class SellerConfiguration
    : IEntityTypeConfiguration<Seller>
{
    public void Configure(
        EntityTypeBuilder<Seller> builder)
    {
        builder.ToTable("Sellers");

        builder.HasKey(x => x.SellerId);

        builder.Property(x => x.StoreName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Bio)
            .HasMaxLength(1000);

        builder.Property(x => x.Rating)
            .HasPrecision(3, 2);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.SellerProfile)
            .HasForeignKey<Seller>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}