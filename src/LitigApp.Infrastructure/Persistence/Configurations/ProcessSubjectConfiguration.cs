using LitigApp.Domain.Processes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class ProcessSubjectConfiguration : IEntityTypeConfiguration<ProcessSubject>
{
    public void Configure(EntityTypeBuilder<ProcessSubject> builder)
    {
        builder.ToTable("process_subjects");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid");
        builder.Property(s => s.ProcessId).IsRequired().HasColumnType("uuid");
        builder.Property(s => s.ExternalSubjectId).HasColumnType("bigint");
        builder.Property(s => s.SubjectType).IsRequired().HasColumnType("text");
        builder.Property(s => s.IsSummoned).HasColumnType("boolean").HasDefaultValue(false);
        builder.Property(s => s.Identification).HasColumnType("text");
        builder.Property(s => s.Name).IsRequired().HasColumnType("text");
        builder.Property(s => s.Source).IsRequired().HasColumnType("text").HasDefaultValue("api");
        builder.Property(s => s.RawPayload).HasColumnType("jsonb");
        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.ProcessId, s.SubjectType })
            .HasDatabaseName("idx_subjects_process_type");
    }
}
