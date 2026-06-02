using LitigApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class UserNotificationPreferencesConfiguration : IEntityTypeConfiguration<UserNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
    {
        builder.ToTable("user_notification_preferences");

        // PK is user_id (text) — no auto-generated value
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId)
            .HasColumnType("text")
            .ValueGeneratedNever();

        builder.Property(p => p.EmailEnabled)
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        // WhatsApp disabled in MVP; default false
        builder.Property(p => p.WhatsAppEnabled)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(p => p.QuietHoursStart).HasColumnType("time");
        builder.Property(p => p.QuietHoursEnd).HasColumnType("time");
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
    }
}
