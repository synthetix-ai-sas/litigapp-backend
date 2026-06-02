using LitigApp.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnType("uuid");
        builder.Property(l => l.OutboxId).HasColumnType("uuid");
        builder.Property(l => l.UserId).IsRequired().HasColumnType("text");
        builder.Property(l => l.EventType).IsRequired().HasColumnType("text");
        builder.Property(l => l.Channel).IsRequired().HasColumnType("text");

        // uuid[] array column
        builder.Property(l => l.ProcessIds).HasColumnType("uuid[]");

        builder.Property(l => l.ProviderMessageId).HasColumnType("text");
        builder.Property(l => l.Status).IsRequired().HasColumnType("text");
        builder.Property(l => l.SentAt).HasColumnType("timestamptz");
        builder.Property(l => l.RawResponse).HasColumnType("jsonb");

        // Index
        builder.HasIndex(l => new { l.UserId, l.SentAt })
            .HasDatabaseName("idx_notif_logs_user_sent")
            .IsDescending(false, true);

        // FK to OutboxMessage (SET NULL on delete)
        builder.HasOne(l => l.Outbox)
            .WithMany()
            .HasForeignKey(l => l.OutboxId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
