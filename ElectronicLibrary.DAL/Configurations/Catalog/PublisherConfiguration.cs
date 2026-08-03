using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class PublisherConfiguration
    : IEntityTypeConfiguration<Publisher>
{
    public void Configure(
        EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");

        builder.HasKey(publisher =>
            publisher.PublisherId);

        builder.Property(publisher =>
                publisher.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(publisher =>
                publisher.Website)
            .HasMaxLength(500);

        builder.Property(publisher =>
                publisher.CreatedAt)
            .HasDefaultValueSql(
                "SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(publisher =>
                publisher.CreatedById)
            .HasMaxLength(450);

        builder.Property(publisher =>
                publisher.UpdatedById)
            .HasMaxLength(450);

        builder.Property(publisher =>
                publisher.DeletedById)
            .HasMaxLength(450);

        builder.Property(publisher =>
                publisher.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(publisher =>
                publisher.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(publisher =>
            !publisher.IsDeleted);

        builder.HasMany(publisher =>
                publisher.Books)
            .WithOne(book =>
                book.Publisher)
            .HasForeignKey(book =>
                book.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}