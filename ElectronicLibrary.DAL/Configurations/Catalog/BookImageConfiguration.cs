using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class BookImageConfiguration
    : IEntityTypeConfiguration<BookImage>
{
    public void Configure(
        EntityTypeBuilder<BookImage> builder)
    {
        builder.ToTable("BookImages");

        builder.HasKey(x => x.BookImageId);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookImages)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}