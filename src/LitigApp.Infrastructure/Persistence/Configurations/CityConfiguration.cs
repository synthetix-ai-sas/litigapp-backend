using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("integer").ValueGeneratedNever();
        builder.Property(c => c.DepartmentId).IsRequired().HasColumnType("smallint");
        builder.Property(c => c.Name).IsRequired().HasColumnType("text");

        builder.HasIndex(c => c.DepartmentId)
            .HasDatabaseName("idx_cities_dept");

        builder.HasOne(c => c.Department)
            .WithMany(d => d.Cities)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
