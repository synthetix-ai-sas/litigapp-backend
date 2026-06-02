using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("entities");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("smallint").ValueGeneratedNever();
        builder.Property(e => e.Code).IsRequired().HasColumnType("char(2)");
        builder.Property(e => e.Name).IsRequired().HasColumnType("text");

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("ix_entities_code");
    }
}
