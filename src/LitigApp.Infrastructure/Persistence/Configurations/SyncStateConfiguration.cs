using LitigApp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("sync_state");

        // PK is text key
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key)
            .HasColumnType("text")
            .ValueGeneratedNever();

        builder.Property(s => s.ValueText).HasColumnType("text");
        builder.Property(s => s.ValueTimestamp).HasColumnType("timestamptz");
        builder.Property(s => s.Reason).HasColumnType("text");
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
    }
}
