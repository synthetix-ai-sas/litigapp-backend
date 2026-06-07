using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("courts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("uuid");

        builder.Property(c => c.OfficialCode).IsRequired().HasColumnType("char(12)").IsFixedLength();
        builder.Property(c => c.CityId).IsRequired().HasColumnType("char(5)").IsFixedLength();
        builder.Property(c => c.EntityCode).HasColumnType("char(2)").IsFixedLength();
        builder.Property(c => c.SpecialtyCode).HasColumnType("char(2)").IsFixedLength();
        builder.Property(c => c.CourtNumber).HasColumnType("smallint");
        builder.Property(c => c.Name).IsRequired().HasColumnType("text");
        builder.Property(c => c.IsActive).HasColumnType("boolean").HasDefaultValue(true);
        builder.Property(c => c.RawPayload).HasColumnType("jsonb");
        builder.Property(c => c.LastSyncedAt).HasColumnType("timestamptz");
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");

        // Unique on official_code
        builder.HasIndex(c => c.OfficialCode)
            .IsUnique()
            .HasDatabaseName("ix_courts_official_code");

        // Composite index city + specialty
        builder.HasIndex(c => new { c.CityId, c.SpecialtyCode })
            .HasDatabaseName("idx_courts_city_spec");

        // GIN trigram index on name (requires pg_trgm extension)
        // operator class added by Npgsql via HasOperators
        builder.HasIndex(c => c.Name)
            .HasDatabaseName("idx_courts_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Relationships
        builder.HasOne(c => c.City)
            .WithMany(ci => ci.Courts)
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.JudicialEntity)
            .WithMany(e => e.Courts)
            .HasForeignKey(c => c.EntityCode)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Specialty)
            .WithMany(s => s.Courts)
            .HasForeignKey(c => c.SpecialtyCode)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
