using System.Text.Json;
using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Maliev.QuotationService.Data.Configurations;

public class QuotationLineItemConfiguration : IEntityTypeConfiguration<QuotationLineItem>
{
    public void Configure(EntityTypeBuilder<QuotationLineItem> builder)
    {
        builder.ToTable("quotation_line_items");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LineNumber).IsRequired();
        builder.HasIndex(l => new { l.VersionId, l.LineNumber }).IsUnique();

        builder.Property(l => l.MaterialServiceId).IsRequired();
        builder.Property(l => l.MaterialName).HasMaxLength(200).IsRequired();

        // Configure JsonDocument property with converter for InMemory database
        var converter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => v == null ? null : JsonDocument.Parse(v));

        builder.Property(l => l.MaterialProperties)
            .HasColumnType("jsonb")
            .HasConversion(converter);

        builder.Property(l => l.ManufacturingProcess).HasMaxLength(100);

        builder.Property(l => l.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.QuantityUnit).HasMaxLength(20).IsRequired();

        builder.Property(l => l.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.LineTotal).HasPrecision(18, 2).IsRequired();

        builder.Property(l => l.Notes).HasMaxLength(500);

        // Version relationship configured in QuotationVersionConfiguration
    }
}
