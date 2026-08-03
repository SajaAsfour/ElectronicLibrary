using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category =>
            category.CategoryId);

        builder.Property(category =>
                category.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(category =>
                category.Description)
            .HasMaxLength(1000);

        builder.Property(category =>
                category.CreatedAt)
            .HasDefaultValueSql(
                "SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(category =>
                category.CreatedById)
            .HasMaxLength(450);

        builder.Property(category =>
                category.UpdatedById)
            .HasMaxLength(450);

        builder.Property(category =>
                category.DeletedById)
            .HasMaxLength(450);

        builder.Property(category =>
                category.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(category =>
                category.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(category =>
            !category.IsDeleted);

        builder.HasMany(category =>
                category.BookCategories)
            .WithOne(bookCategory =>
                bookCategory.Category)
            .HasForeignKey(bookCategory =>
                bookCategory.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}