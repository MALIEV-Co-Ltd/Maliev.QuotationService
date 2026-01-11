using System.Text.Json;
using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Maliev.QuotationService.Data.Configurations;

public class RfqConfiguration : IEntityTypeConfiguration<Rfq>
{
    public void Configure(EntityTypeBuilder<Rfq> builder)
    {
        builder.ToTable("rfqs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ChannelSource).IsRequired();
        builder.HasIndex(r => r.ChannelSource);

        builder.Property(r => r.Status).IsRequired();
        builder.HasIndex(r => r.Status);

        builder.HasIndex(r => new { r.ChannelSource, r.Status, r.CreatedAt });

        builder.Property(r => r.RequestDetails)
            .HasColumnType("jsonb");

        builder.Property(r => r.AssignedStaffUserId).HasMaxLength(50);
        builder.HasIndex(r => r.AssignedStaffUserId);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(r => r.CreatedAt);

        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(r => r.IsDeleted);

        builder.HasQueryFilter(r => !r.IsDeleted); // Global query filter

        // Customer relationship configured in CustomerConfiguration

        // ConvertedToQuotation relationship
        builder.HasOne(r => r.ConvertedToQuotation)
            .WithMany()
            .HasForeignKey(r => r.ConvertedToQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        // InternalNotes relationship
        builder.HasMany(r => r.InternalNotes)
            .WithOne(n => n.Rfq)
            .HasForeignKey(n => n.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        // FileReferences relationship
        builder.HasMany(r => r.FileReferences)
            .WithOne(f => f.Rfq)
            .HasForeignKey(f => f.RfqId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
