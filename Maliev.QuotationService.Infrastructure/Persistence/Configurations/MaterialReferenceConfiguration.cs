using Maliev.QuotationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Infrastructure.Persistence.Configurations;

public class MaterialReferenceConfiguration : IEntityTypeConfiguration<MaterialReference>
{
    public void Configure(EntityTypeBuilder<MaterialReference> builder)
    {
        builder.ToTable("material_references");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MaterialServiceId).IsRequired();
        builder.HasIndex(m => m.MaterialServiceId).IsUnique();

        builder.Property(m => m.MaterialName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.MaterialCategory).HasMaxLength(100);

        builder.Property(m => m.PhysicalProperties)
            .HasColumnType("jsonb");

        builder.Property(m => m.MechanicalProperties)
            .HasColumnType("jsonb");

        builder.Property(m => m.AvailabilityStatus).IsRequired();

        builder.Property(m => m.CachedAt).HasDefaultValueSql("NOW()");
        builder.Property(m => m.ExpiresAt).IsRequired();

        builder.HasIndex(m => m.ExpiresAt);
    }
}
