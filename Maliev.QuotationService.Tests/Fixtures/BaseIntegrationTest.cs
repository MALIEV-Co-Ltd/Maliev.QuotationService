using System.Net.Http.Headers;
using Maliev.QuotationService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.QuotationService.Tests.Fixtures;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly QuotationDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<QuotationDbContext>();
    }

    /// <summary>
    /// Creates an authenticated HTTP client with a test JWT token.
    /// </summary>
    /// <param name="userId">User ID for the token</param>
    /// <param name="roles">User roles (defaults to ["Employee"])</param>
    /// <returns>HttpClient with Authorization header set</returns>
    protected HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null)
    {
        var client = Factory.CreateClient();
        var token = Factory.CreateTestJwtToken(userId, roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        // Clean up database after each test to ensure isolation
        // Clear the change tracker to avoid navigation fixup issues
        DbContext.ChangeTracker.Clear();

        // Delete all data from tables in correct order (respecting foreign keys)
        // First null out CurrentVersionId to avoid FK constraint violations
        await DbContext.Database.ExecuteSqlAsync($"UPDATE quotations SET \"CurrentVersionId\" = NULL");

        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM file_references");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM internal_notes");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM discount_structures");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM quotation_line_items");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM quotation_versions");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM quotations");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM material_references");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM rfqs");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM customers");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM staff_roles");
        await DbContext.Database.ExecuteSqlAsync($"DELETE FROM audit_log_entries");

        Scope.Dispose();
        Client.Dispose();
    }
}
