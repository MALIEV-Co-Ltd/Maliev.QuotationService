using Maliev.QuotationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Infrastructure.Persistence.Configurations;

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

        builder.Property(l => l.MaterialProperties)
            .HasColumnType("jsonb");

        builder.Property(l => l.ManufacturingProcess).HasMaxLength(100);

        builder.Property(l => l.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.QuantityUnit).HasMaxLength(20).IsRequired();

        builder.Property(l => l.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.LineTotal).HasPrecision(18, 2).IsRequired();

        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasQueryFilter(l => !l.Version.Quotation.IsDeleted);
    }
}
