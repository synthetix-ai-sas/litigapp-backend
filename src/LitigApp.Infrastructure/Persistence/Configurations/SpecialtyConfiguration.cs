using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("smallint").ValueGeneratedNever();
        builder.Property(s => s.Code).IsRequired().HasColumnType("char(2)");
        builder.Property(s => s.Name).IsRequired().HasColumnType("text");

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("ix_specialties_code");
    }
}
