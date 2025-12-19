using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.QuotationService.Data;

/// <summary>
/// Design-time factory for creating QuotationDbContext instances during migrations.
/// </summary>
public class QuotationDbContextFactory : IDesignTimeDbContextFactory<QuotationDbContext>
{
    public QuotationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuotationDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection string is provided at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=quotation_design;Username=postgres;Password=postgres");

        return new QuotationDbContext(optionsBuilder.Options);
    }
}
