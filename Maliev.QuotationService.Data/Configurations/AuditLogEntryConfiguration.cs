using System.Text.Json;
using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Maliev.QuotationService.Data.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired();
        builder.HasIndex(a => a.EntityType);

        builder.Property(a => a.EntityId).IsRequired();
        builder.HasIndex(a => a.EntityId);

        builder.Property(a => a.UserId).HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.ActionType).IsRequired();
        builder.HasIndex(a => a.ActionType);

        builder.Property(a => a.Timestamp).HasDefaultValueSql("NOW()");
        builder.HasIndex(a => a.Timestamp);

        // Configure JsonDocument property with converter for InMemory database
        var converter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => v == null ? null : JsonDocument.Parse(v));

        builder.Property(a => a.ChangedFields)
            .HasColumnType("jsonb")
            .HasConversion(converter);

        builder.Property(a => a.IpAddress).HasMaxLength(45); // IPv6 max length
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        // Composite index for common queries
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.Timestamp });
    }
}
