using Maliev.QuotationService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Data;

public class QuotationDbContext : DbContext
{
    public QuotationDbContext(DbContextOptions<QuotationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Rfq> Rfqs { get; set; } = null!;
    public DbSet<Quotation> Quotations { get; set; } = null!;
    public DbSet<QuotationVersion> QuotationVersions { get; set; } = null!;
    public DbSet<QuotationLineItem> QuotationLineItems { get; set; } = null!;
    public DbSet<DiscountStructure> DiscountStructures { get; set; } = null!;
    public DbSet<InternalNote> InternalNotes { get; set; } = null!;
    public DbSet<FileReference> FileReferences { get; set; } = null!;
    public DbSet<MaterialReference> MaterialReferences { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;
    public DbSet<StaffRole> StaffRoles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuotationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update UpdatedAt timestamps automatically
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.GetType().GetProperty("CreatedAt") != null)
                {
                    var createdAt = entry.Property("CreatedAt").CurrentValue;
                    if (createdAt == null || (DateTime)createdAt == default)
                    {
                        entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
