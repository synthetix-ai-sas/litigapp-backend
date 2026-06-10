using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");

        builder.HasKey(s => s.Code);
        builder.Property(s => s.Code).HasColumnType("char(2)").IsFixedLength().ValueGeneratedNever();
        builder.Property(s => s.Name).IsRequired().HasColumnType("text");
    }
}
