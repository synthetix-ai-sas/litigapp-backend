using LitigApp.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("notifications_outbox");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnType("uuid");
        builder.Property(o => o.UserId).IsRequired().HasColumnType("text");
        builder.Property(o => o.EventType).IsRequired().HasColumnType("text");
        builder.Property(o => o.Channel).IsRequired().HasColumnType("text");
        builder.Property(o => o.Payload).IsRequired().HasColumnType("jsonb");

        builder.Property(o => o.Status)
            .IsRequired()
            .HasColumnType("text")
            .HasDefaultValue("pending");

        builder.Property(o => o.Attempts)
            .HasColumnType("smallint")
            .HasDefaultValue((short)0);

        builder.Property(o => o.LastError).HasColumnType("text");
        builder.Property(o => o.CreatedAt).HasColumnType("timestamptz");
        builder.Property(o => o.ProcessedAt).HasColumnType("timestamptz");

        // Filtered index: only pending/processing rows
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("idx_outbox_status_created")
            .HasFilter("status IN ('pending', 'processing')");
    }
}
