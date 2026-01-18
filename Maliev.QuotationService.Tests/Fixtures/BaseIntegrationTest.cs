using System.Net.Http.Headers;
using Maliev.QuotationService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.QuotationService.Tests.Fixtures;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncLifetime
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
        var effectiveRoles = roles ?? new[] { "Employee" };
        var claims = new Dictionary<string, string>();

        // Map roles to permissions for tests to pass with new permission-based auth
        // Use a simple mapping for common test roles
        var permissions = new List<string>();
        foreach (var role in effectiveRoles)
        {
            if (role == "Admin" || role == "quotation-admin")
                permissions.AddRange(Maliev.QuotationService.Api.Services.IAM.QuotationPermissions.All);
            else if (role == "Manager" || role == "quotation-manager")
                permissions.AddRange(new[] {
                    "quotation.quotations.create", "quotation.quotations.read", "quotation.quotations.update",
                    "quotation.quotations.approve", "quotation.quotations.delete" });
            else if (role == "Employee" || role == "quotation-creator")
                permissions.AddRange(new[] {
                    "quotation.quotations.create", "quotation.quotations.read", "quotation.quotations.update" });
            else if (role == "Customer" || role == "quotation-viewer")
                permissions.Add("quotation.quotations.read");
        }

        foreach (var perm in permissions.Distinct())
        {
            // Note: Multiple claims with same key are supported by the factory
            claims.Add($"perm_{perm}", perm);
        }

        // Adjust factory to handle multiple permissions if needed,
        // but current factory takes Dictionary<string, string> which limits to one value per key.
        // I need to update the factory to support multiple claims of same type.

        var token = Factory.CreateTestJwtToken(userId, effectiveRoles, permissions.Distinct().ToArray());
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public virtual async Task InitializeAsync()
    {
        // Ensure containers are started and migrations applied
        await Factory.InitializeAsync();
        // Clean database for test isolation
        await Factory.CleanDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        // Cleanup is handled by next test's constructor via Factory.CleanDatabaseAsync()
        // This avoids connection issues with TRUNCATE CASCADE on active connections
        Scope.Dispose();
        Client.Dispose();
        return Task.CompletedTask;
    }
}
