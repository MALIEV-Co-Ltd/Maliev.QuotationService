using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email).HasMaxLength(320).IsRequired(false);
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired(false);
        builder.HasIndex(c => c.PhoneNumber);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Name);

        builder.Property(c => c.ContactInfo)
            .HasColumnType("jsonb");

        builder.Property(c => c.MergeHistory)
            .HasColumnType("jsonb");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(c => c.IsDeleted);

        builder.HasQueryFilter(c => !c.IsDeleted); // Global query filter

        // Navigation properties
        builder.HasMany(c => c.Rfqs)
            .WithOne(r => r.Customer)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Quotations)
            .WithOne(q => q.Customer)
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
