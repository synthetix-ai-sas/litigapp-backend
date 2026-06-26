using LitigApp.Domain.Processes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class ProcessConfiguration : IEntityTypeConfiguration<Process>
{
    public void Configure(EntityTypeBuilder<Process> builder)
    {
        builder.ToTable("processes", t =>
            t.HasCheckConstraint("chk_processes_file_length", "length(file_number) = 23"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnType("uuid");

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(p => p.FileNumber)
            .IsRequired()
            .HasColumnType("char(23)");

        builder.Property(p => p.ExternalProcessId).HasColumnType("bigint");
        builder.Property(p => p.ExternalConnectionId).HasColumnType("integer");

        builder.Property(p => p.CourtId).HasColumnType("uuid");
        builder.Property(p => p.FilingYear).HasColumnType("smallint");

        builder.Property(p => p.ProcessType).HasColumnType("text");
        builder.Property(p => p.ProcessClass).HasColumnType("text");
        builder.Property(p => p.ProcessSubclass).HasColumnType("text");
        builder.Property(p => p.Resource).HasColumnType("text");
        builder.Property(p => p.JudgeName).HasColumnType("text");
        builder.Property(p => p.FilingContent).HasColumnType("text");
        builder.Property(p => p.IsPrivate).HasColumnType("boolean").HasDefaultValue(false);
        builder.Property(p => p.CustomAlias).HasColumnType("text");

        builder.Property(p => p.CurrentStatus).HasColumnType("text");
        builder.Property(p => p.LastCourtActionAt).HasColumnType("timestamptz");
        builder.Property(p => p.LastSyncedAt).HasColumnType("timestamptz");
        builder.Property(p => p.LastSyncAttemptAt).HasColumnType("timestamptz");

        builder.Property(p => p.LastExternalConsecutive)
            .HasColumnType("integer")
            .HasDefaultValue(0);

        builder.Property(p => p.SyncStatus)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue("pending");

        builder.Property(p => p.SyncPhase)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue(ProcessSyncPhase.PendingInitialFull);

        builder.Property(p => p.SyncError).HasColumnType("text");

        builder.Property(p => p.SyncAttempts)
            .HasColumnType("integer")
            .HasDefaultValue(0);

        builder.Property(p => p.Attended)
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(p => p.IsActive)
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");

        // Unique constraint
        builder.HasIndex(p => new { p.UserId, p.FileNumber })
            .IsUnique()
            .HasDatabaseName("uq_processes_user_file");

        // Indexes
        builder.HasIndex(p => new { p.UserId, p.Attended, p.LastCourtActionAt })
            .HasDatabaseName("idx_processes_user_attended")
            .IsDescending(false, false, true);

        builder.HasIndex(p => new { p.UserId, p.IsActive, p.LastCourtActionAt })
            .HasDatabaseName("idx_processes_user_active")
            .IsDescending(false, false, true);

        // Filtered index: WHERE is_active = true
        // Note: NULLS FIRST on last_sync_attempt_at must be added manually in the migration SQL
        builder.HasIndex(p => new { p.SyncPhase, p.LastSyncAttemptAt })
            .HasDatabaseName("idx_processes_sync_phase")
            .HasFilter("is_active = true");

        builder.HasIndex(p => p.ExternalProcessId)
            .HasDatabaseName("idx_processes_external");

        // Relationships
        builder.HasOne(p => p.Court)
            .WithMany()
            .HasForeignKey(p => p.CourtId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Actions)
            .WithOne(a => a.Process)
            .HasForeignKey(a => a.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Subjects)
            .WithOne(s => s.Process)
            .HasForeignKey(s => s.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
