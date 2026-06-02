using LitigApp.Domain.Processes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class ProcessActionConfiguration : IEntityTypeConfiguration<ProcessAction>
{
    public void Configure(EntityTypeBuilder<ProcessAction> builder)
    {
        builder.ToTable("process_actions");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnType("uuid");

        builder.Property(a => a.ProcessId).IsRequired().HasColumnType("uuid");
        builder.Property(a => a.ExternalActionId).IsRequired().HasColumnType("bigint");
        builder.Property(a => a.ConsecutiveNumber).IsRequired().HasColumnType("integer");

        builder.Property(a => a.ActionDate).HasColumnType("date");
        builder.Property(a => a.Action).HasColumnType("text");
        builder.Property(a => a.Annotation).HasColumnType("text");
        builder.Property(a => a.TermStartDate).HasColumnType("date");
        builder.Property(a => a.TermEndDate).HasColumnType("date");
        builder.Property(a => a.RecordedAt).HasColumnType("date");

        builder.Property(a => a.HasDocuments)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(a => a.RuleCode).HasColumnType("text");
        builder.Property(a => a.GroupedWithId).HasColumnType("uuid");

        builder.Property(a => a.RawPayload).HasColumnType("jsonb");
        builder.Property(a => a.CreatedAt).HasColumnType("timestamptz");

        // Unique constraint
        builder.HasIndex(a => new { a.ProcessId, a.ExternalActionId })
            .IsUnique()
            .HasDatabaseName("uq_actions_process_external");

        // Indexes
        builder.HasIndex(a => new { a.ProcessId, a.ConsecutiveNumber })
            .HasDatabaseName("idx_actions_process_consec")
            .IsDescending(false, true);

        builder.HasIndex(a => new { a.ProcessId, a.RecordedAt })
            .HasDatabaseName("idx_actions_process_recorded");

        // Self-reference: Fijación → Auto
        builder.HasOne(a => a.GroupedWith)
            .WithMany()
            .HasForeignKey(a => a.GroupedWithId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
