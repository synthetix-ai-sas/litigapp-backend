using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitigApp.Infrastructure.Persistence.Configurations;

public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("entities");

        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasColumnType("char(2)").IsFixedLength().ValueGeneratedNever();
        builder.Property(e => e.Name).IsRequired().HasColumnType("text");
    }
}
