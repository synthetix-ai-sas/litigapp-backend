using LitigApp.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public sealed class LegalAcceptanceConfiguration : IEntityTypeConfiguration<LegalAcceptance>
{
    public void Configure(EntityTypeBuilder<LegalAcceptance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DocumentType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.DocumentVersion).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AcceptedAt).IsRequired();
        builder.Property(x => x.IpAddress).HasColumnType("inet");
        builder.HasIndex(x => x.UserId);

        builder.HasOne<LitigApp.Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
