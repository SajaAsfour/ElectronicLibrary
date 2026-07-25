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

        builder.HasKey(x => x.BookId);

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Isbn)
            .HasMaxLength(20);

        builder.Property(x => x.Description)
            .HasMaxLength(3000);

        builder.Property(x => x.Language)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Isbn)
            .IsUnique()
            .HasFilter("[Isbn] IS NOT NULL");

        builder.HasOne(x => x.Publisher)
            .WithMany(x => x.Books)
            .HasForeignKey(x => x.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}