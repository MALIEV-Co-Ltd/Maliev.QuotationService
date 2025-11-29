using System.Text.Json;
using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Maliev.QuotationService.Data.Configurations;

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

        // Configure JsonDocument properties with converter for InMemory database
        var converter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => v == null ? null : JsonDocument.Parse(v));

        builder.Property(m => m.PhysicalProperties)
            .HasColumnType("jsonb")
            .HasConversion(converter);

        builder.Property(m => m.MechanicalProperties)
            .HasColumnType("jsonb")
            .HasConversion(converter);

        builder.Property(m => m.AvailabilityStatus).IsRequired();

        builder.Property(m => m.CachedAt).HasDefaultValueSql("NOW()");
        builder.Property(m => m.ExpiresAt).IsRequired();

        builder.HasIndex(m => m.ExpiresAt);
    }
}
