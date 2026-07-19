using Maliev.QuotationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Infrastructure.Persistence.Configurations;

public class FileReferenceConfiguration : IEntityTypeConfiguration<FileReference>
{
    public void Configure(EntityTypeBuilder<FileReference> builder)
    {
        builder.ToTable("file_references", t =>
        {
            t.HasCheckConstraint(
                "CK_FileReference_Entity",
                "(\"rfq_id\" IS NOT NULL AND \"quotation_id\" IS NULL) OR (\"rfq_id\" IS NULL AND \"quotation_id\" IS NOT NULL)"
            );
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UploadServiceFileId).IsRequired();
        builder.HasIndex(f => f.UploadServiceFileId).IsUnique();

        builder.Property(f => f.FileName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.FileType).HasMaxLength(100).IsRequired();

        builder.Property(f => f.UploadedAt).HasDefaultValueSql("NOW()");
        builder.Property(f => f.UploadedByUserId).HasMaxLength(50).IsRequired();

        builder.HasIndex(f => f.RfqId);
        builder.HasIndex(f => f.QuotationId);
    }
}
