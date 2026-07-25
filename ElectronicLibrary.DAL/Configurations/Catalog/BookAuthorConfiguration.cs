using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class BookAuthorConfiguration
    : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(
        EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");

        builder.HasKey(x => new
        {
            x.BookId,
            x.AuthorId
        });

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}