using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.QuotationService.Infrastructure.Persistence;

public class QuotationDbContextFactory : IDesignTimeDbContextFactory<QuotationDbContext>
{
    public QuotationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuotationDbContext>();

        optionsBuilder.UseNpgsql("Host=localhost;Database=quotation_design;Username=postgres;Password=postgres");

        return new QuotationDbContext(optionsBuilder.Options);
    }
}
