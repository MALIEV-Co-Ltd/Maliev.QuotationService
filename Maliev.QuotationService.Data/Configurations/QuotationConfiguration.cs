using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Data.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Status).IsRequired();
        builder.HasIndex(q => q.Status);

        builder.Property(q => q.ValidityPeriodStart).IsRequired();
        builder.Property(q => q.ValidityPeriodEnd).IsRequired();
        builder.HasIndex(q => q.ValidityPeriodEnd);

        builder.Property(q => q.RowVersion).IsRowVersion();

        builder.Property(q => q.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(q => q.UpdatedAt).IsRequired();
        builder.Property(q => q.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(q => q.IsDeleted);

        builder.HasQueryFilter(q => !q.IsDeleted); // Global query filter

        // Customer relationship configured in CustomerConfiguration

        // SourceRfq relationship
        builder.HasOne(q => q.SourceRfq)
            .WithMany()
            .HasForeignKey(q => q.SourceRfqId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.SourceRfqId);

        // CurrentVersion relationship
        builder.HasOne(q => q.CurrentVersion)
            .WithMany()
            .HasForeignKey(q => q.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Versions relationship
        builder.HasMany(q => q.Versions)
            .WithOne(v => v.Quotation)
            .HasForeignKey(v => v.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        // InternalNotes relationship
        builder.HasMany(q => q.InternalNotes)
            .WithOne(n => n.Quotation)
            .HasForeignKey(n => n.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        // FileReferences relationship
        builder.HasMany(q => q.FileReferences)
            .WithOne(f => f.Quotation)
            .HasForeignKey(f => f.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
