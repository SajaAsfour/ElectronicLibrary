using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class BookCategoryConfiguration
    : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(
        EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("BookCategories");

        builder.HasKey(x => new
        {
            x.BookId,
            x.CategoryId
        });

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookCategories)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.BookCategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(
            bookCategory =>
                !bookCategory.Book.IsDeleted &&
                !bookCategory.Category.IsDeleted);
    }
}