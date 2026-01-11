using System.Text.Json;
using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Maliev.QuotationService.Data.Configurations;

public class QuotationVersionConfiguration : IEntityTypeConfiguration<QuotationVersion>
{
    public void Configure(EntityTypeBuilder<QuotationVersion> builder)
    {
        builder.ToTable("quotation_versions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionNumber).IsRequired();
        builder.HasIndex(v => new { v.QuotationId, v.VersionNumber }).IsUnique();

        builder.Property(v => v.CreatedByUserId).HasMaxLength(50).IsRequired();
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("NOW()");

        builder.Property(v => v.ChangeSummary).HasMaxLength(1000);

        builder.Property(v => v.TotalPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(v => v.CurrencyCode).HasMaxLength(3).IsRequired();

        builder.Property(v => v.DeliveryExpectations)
            .HasColumnType("jsonb");

        builder.Property(v => v.SpecialTerms).HasMaxLength(2000);

        // Quotation relationship configured in QuotationConfiguration

        // LineItems relationship
        builder.HasMany(v => v.LineItems)
            .WithOne(l => l.Version)
            .HasForeignKey(l => l.VersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // DiscountStructures relationship
        builder.HasMany(v => v.DiscountStructures)
            .WithOne(d => d.QuotationVersion)
            .HasForeignKey(d => d.QuotationVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Matching query filter to avoid warnings with required relationship
        builder.HasQueryFilter(v => !v.Quotation.IsDeleted);
    }
}
