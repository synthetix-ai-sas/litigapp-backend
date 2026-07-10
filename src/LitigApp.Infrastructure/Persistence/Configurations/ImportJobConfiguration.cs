using LitigApp.Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("import_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnType("uuid");
        builder.Property(j => j.UserId).IsRequired().HasColumnType("text");
        builder.Property(j => j.FileName).IsRequired().HasColumnType("text");

        builder.Property(j => j.TotalRows).HasColumnType("integer").HasDefaultValue(0);
        builder.Property(j => j.ProcessedRows).HasColumnType("integer").HasDefaultValue(0);
        builder.Property(j => j.SuccessCount).HasColumnType("integer").HasDefaultValue(0);
        builder.Property(j => j.ErrorCount).HasColumnType("integer").HasDefaultValue(0);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue("pending");

        builder.Property(j => j.ColumnMapping).HasColumnType("jsonb");
        builder.Property(j => j.PreviewPayload).HasColumnType("jsonb");
        builder.Property(j => j.Errors).HasColumnType("jsonb");
        builder.Property(j => j.SyncError).HasColumnType("text");
        builder.Property(j => j.PreviewId).HasColumnType("uuid");
        builder.Property(j => j.CreatedAt).HasColumnType("timestamptz");
        builder.Property(j => j.CompletedAt).HasColumnType("timestamptz");

        builder.HasIndex(j => new { j.UserId, j.CreatedAt })
            .HasDatabaseName("idx_imports_user_created")
            .IsDescending(false, true);
    }
}
