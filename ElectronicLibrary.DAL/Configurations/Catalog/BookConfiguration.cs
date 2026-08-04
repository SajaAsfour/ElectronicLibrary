using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class BookConfiguration
    : IEntityTypeConfiguration<Book>
{
    public void Configure(
        EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(book => book.BookId);

        builder.Property(book => book.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(book => book.Isbn)
            .HasMaxLength(20);

        builder.Property(book => book.Description)
            .HasMaxLength(3000);

        builder.Property(book => book.Language)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(book => book.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(book => book.CreatedById)
            .HasMaxLength(450);

        builder.Property(book => book.UpdatedById)
            .HasMaxLength(450);

        builder.Property(book => book.DeletedById)
            .HasMaxLength(450);

        builder.Property(book => book.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(book => book.Isbn)
            .IsUnique()
            .HasFilter(
                "[Isbn] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasQueryFilter(
            book =>
                !book.IsDeleted &&
                !book.Publisher.IsDeleted);

        builder.HasOne(book => book.Publisher)
            .WithMany(publisher => publisher.Books)
            .HasForeignKey(book => book.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}