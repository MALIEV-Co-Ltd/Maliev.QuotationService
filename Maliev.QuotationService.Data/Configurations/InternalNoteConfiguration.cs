using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Data.Configurations;

public class InternalNoteConfiguration : IEntityTypeConfiguration<InternalNote>
{
    public void Configure(EntityTypeBuilder<InternalNote> builder)
    {
        builder.ToTable("internal_notes", t =>
        {
            t.HasCheckConstraint(
                "CK_InternalNote_Entity",
                "(\"rfq_id\" IS NOT NULL AND \"quotation_id\" IS NULL) OR (\"rfq_id\" IS NULL AND \"quotation_id\" IS NOT NULL)"
            );
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.AuthorUserId).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Content).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(n => n.RfqId);
        builder.HasIndex(n => n.QuotationId);
        builder.HasIndex(n => n.CreatedAt);

        // Relationships configured in RfqConfiguration and QuotationConfiguration
    }
}
