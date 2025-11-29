using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.QuotationService.Data.Configurations;

public class StaffRoleConfiguration : IEntityTypeConfiguration<StaffRole>
{
    public void Configure(EntityTypeBuilder<StaffRole> builder)
    {
        builder.ToTable("staff_roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.RoleName).IsUnique();

        builder.Property(r => r.Permissions).IsRequired();

        builder.Property(r => r.Description).HasMaxLength(500);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
