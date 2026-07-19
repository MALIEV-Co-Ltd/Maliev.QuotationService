using System.Net.Http.Headers;
using Maliev.QuotationService.Infrastructure.Persistence;
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

        // Map short role names to full role IDs
        var roleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Admin", "roles.quotation.admin" },
            { "Manager", "roles.quotation.manager" },
            { "Creator", "roles.quotation.creator" },
            { "Viewer", "roles.quotation.viewer" },
            { "Employee", "roles.quotation.creator" },
            { "Customer", "roles.quotation.viewer" },
            { "quotation-admin", "roles.quotation.admin" },
            { "quotation-manager", "roles.quotation.manager" },
            { "quotation-creator", "roles.quotation.creator" },
            { "quotation-viewer", "roles.quotation.viewer" }
        };

        var mappedRoles = effectiveRoles.Select(r => roleMapping.TryGetValue(r, out var mapped) ? mapped : r).ToArray();

        // Map roles to permissions for tests to pass with new permission-based auth
        var permissions = new List<string>();
        foreach (var role in mappedRoles)
        {
            if (role == "roles.quotation.admin")
                permissions.AddRange(Maliev.QuotationService.Application.Authorization.QuotationPermissions.All);
            else if (role == "roles.quotation.manager")
                permissions.AddRange(new[] {
                    "quotation.quotations.create", "quotation.quotations.read", "quotation.quotations.update",
                    "quotation.quotations.approve", "quotation.quotations.delete", "quotation.quotations.send" });
            else if (role == "roles.quotation.creator")
                permissions.AddRange(new[] {
                    "quotation.quotations.create", "quotation.quotations.read", "quotation.quotations.update",
                    "quotation.quotations.send", "quotation.lineitems.create", "quotation.lineitems.read",
                    "quotation.templates.read", "quotation.templates.use" });
            else if (role == "roles.quotation.viewer")
                permissions.Add("quotation.quotations.read");
        }

        var token = Factory.CreateTestJwtToken(userId, mappedRoles, permissions.Distinct().ToArray());
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public virtual async Task InitializeAsync()
    {
        Factory.ResetTestDoubles();
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
