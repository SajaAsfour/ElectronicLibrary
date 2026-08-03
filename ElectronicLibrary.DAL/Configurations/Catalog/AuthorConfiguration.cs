using ElectronicLibrary.DAL.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicLibrary.DAL.Configurations.Catalog;

public class AuthorConfiguration
    : IEntityTypeConfiguration<Author>
{
    public void Configure(
        EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(x => x.AuthorId);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Biography)
            .HasMaxLength(3000);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.CreatedById)
            .HasMaxLength(450);

        builder.Property(x => x.UpdatedById)
            .HasMaxLength(450);

        builder.Property(x => x.DeletedById)
            .HasMaxLength(450);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}