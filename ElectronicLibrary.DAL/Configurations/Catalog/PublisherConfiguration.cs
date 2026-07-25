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

        builder.HasKey(x => x.PublisherId);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Website)
            .HasMaxLength(500);
    }
}