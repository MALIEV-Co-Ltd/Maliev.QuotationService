using Maliev.QuotationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.QuotationService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<QuotationDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new QuotationDbContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.QuotationService.Infrastructure --startup-project Maliev.QuotationService.Api'");
    }
}
