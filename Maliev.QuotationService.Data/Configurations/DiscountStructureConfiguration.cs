using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Data.Configurations;

public class DiscountStructureConfiguration : IEntityTypeConfiguration<DiscountStructure>
{
    public void Configure(EntityTypeBuilder<DiscountStructure> builder)
    {
        builder.ToTable("discount_structures");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DiscountType).IsRequired();
        builder.Property(d => d.DiscountValue).HasPrecision(18, 2).IsRequired();

        builder.Property(d => d.Conditions).HasMaxLength(500);
        builder.Property(d => d.AuthorizationReason).HasMaxLength(500);

        builder.HasIndex(d => d.QuotationVersionId);


        // QuotationVersion relationship configured in QuotationVersionConfiguration

        // Matching query filter to avoid warnings with required relationship
        builder.HasQueryFilter(d => !d.QuotationVersion.Quotation.IsDeleted);
    }
}
