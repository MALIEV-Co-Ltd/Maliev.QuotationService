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

        // Clean database for test isolation using TRUNCATE (not DROP/CREATE)
        Factory.CleanDatabaseAsync().GetAwaiter().GetResult();
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

    public virtual Task DisposeAsync()
    {
        // Cleanup is handled by next test's constructor via Factory.CleanDatabaseAsync()
        // This avoids connection issues with TRUNCATE CASCADE on active connections
        Scope.Dispose();
        Client.Dispose();
        return Task.CompletedTask;
    }
}
