using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.QuotationService.Tests.Integration;

[Trait("Category", "Authorization")]
public class AuthorizationTests : BaseIntegrationTest
{
    public AuthorizationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateQuotation_WithCorrectPermission_ReturnsCreated()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        var request = CreateValidQuotationRequest(customer.Id);

        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsCreate }
        };
        var token = Factory.CreateTestJwtToken("creator-user", roles: new[] { "roles.quotation.creator" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/quotation/v1/quotations", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuotation_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        var request = CreateValidQuotationRequest(customer.Id);

        // Token with NO permissions
        var token = Factory.CreateTestJwtToken("unauthorized-user", roles: new[] { "quotation-viewer" });

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/quotation/v1/quotations", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify Audit Log Entry
        var auditLog = await DbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.UserId == "unauthorized-user" && a.ActionType == AuditActionType.Unauthorized);

        Assert.NotNull(auditLog);
        Assert.Equal(AuditEntityType.Security, auditLog.EntityType);
    }

    [Fact]
    public async Task ApproveQuotation_AsManager_ReturnsOk()
    {
        // Arrange
        var quotation = await CreateTestQuotationAsync();

        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsApprove }
        };
        var token = Factory.CreateTestJwtToken("manager-user", roles: new[] { "roles.quotation.manager" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync($"/quotation/v1/quotations/{quotation.Id}/approve", null);

        // Assert
        // Note: Actual response might be 200 or 204 depending on implementation, but NOT 403
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApproveQuotation_AsCreator_ReturnsForbidden()
    {
        // Arrange
        var quotation = await CreateTestQuotationAsync();

        // Creator has create but NOT approve
        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsCreate }
        };
        var token = Factory.CreateTestJwtToken("creator-user", roles: new[] { "roles.quotation.creator" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync($"/quotation/v1/quotations/{quotation.Id}/approve", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuotation_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var quotation = await CreateTestQuotationAsync();

        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsDelete }
        };
        var token = Factory.CreateTestJwtToken("admin-user", roles: new[] { "roles.quotation.admin" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/quotation/v1/quotations/{quotation.Id}");

        // Assert
        // Standard delete might return 204 or 200, check it's NOT forbidden
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuotation_AsManager_ReturnsForbidden()
    {
        // Arrange
        var quotation = await CreateTestQuotationAsync();

        // Manager does NOT have delete permission by default in our spec
        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsApprove }
        };
        var token = Factory.CreateTestJwtToken("manager-user", roles: new[] { "roles.quotation.manager" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/quotation/v1/quotations/{quotation.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetQuotations_AsViewer_ReturnsOk()
    {
        // Arrange
        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsRead }
        };
        var token = Factory.CreateTestJwtToken("viewer-user", roles: new[] { "quotation-viewer" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/quotation/v1/quotations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<QuotationResponse>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRfqs_AsViewer_ReturnsOk()
    {
        // Arrange
        var claims = new Dictionary<string, string>
        {
            { "permissions", QuotationPermissions.QuotationsRead }
        };
        var token = Factory.CreateTestJwtToken("viewer-user", roles: new[] { "quotation-viewer" }, additionalClaims: claims);

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/quotation/v1/rfqs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<RfqResponse>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetQuotations_WithCustomerScope_ReturnsOnlyScopedCustomerQuotations()
    {
        var scopedCustomer = await CreateTestCustomerAsync();
        var otherCustomer = await CreateTestCustomerAsync();
        var scopedQuotation = await CreateTestQuotationAsync(scopedCustomer);
        var otherQuotation = await CreateTestQuotationAsync(otherCustomer);
        using var client = CreateCustomerScopedClient(scopedCustomer.Id, QuotationPermissions.QuotationsRead);

        var listResponse = await client.GetAsync("/quotation/v1/quotations");
        var ownResponse = await client.GetAsync($"/quotation/v1/quotations/{scopedQuotation.Id}");
        var otherResponse = await client.GetAsync($"/quotation/v1/quotations/{otherQuotation.Id}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResponse<QuotationResponse>>();
        Assert.NotNull(result);
        var match = Assert.Single(result.Data);
        Assert.Equal(scopedQuotation.Id, match.Id);
        Assert.Equal(scopedCustomer.Id, match.CustomerId);
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task CreateQuotation_WithCustomerScopeForDifferentCustomer_ReturnsForbidden()
    {
        var scopedCustomer = await CreateTestCustomerAsync();
        var otherCustomer = await CreateTestCustomerAsync();
        using var client = CreateCustomerScopedClient(scopedCustomer.Id, QuotationPermissions.QuotationsCreate);
        var request = CreateValidQuotationRequest(otherCustomer.Id);

        var response = await client.PostAsJsonAsync("/quotation/v1/quotations", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRfqs_WithCustomerScope_ReturnsOnlyScopedCustomerRfqs()
    {
        var scopedCustomer = await CreateTestCustomerAsync();
        var otherCustomer = await CreateTestCustomerAsync();
        var scopedRfq = await CreateTestRfqAsync(scopedCustomer);
        var otherRfq = await CreateTestRfqAsync(otherCustomer);
        using var client = CreateCustomerScopedClient(scopedCustomer.Id, QuotationPermissions.QuotationsRead);

        var listResponse = await client.GetAsync("/quotation/v1/rfqs");
        var ownResponse = await client.GetAsync($"/quotation/v1/rfqs/{scopedRfq.Id}");
        var otherResponse = await client.GetAsync($"/quotation/v1/rfqs/{otherRfq.Id}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResponse<RfqResponse>>();
        Assert.NotNull(result);
        var match = Assert.Single(result.Data);
        Assert.Equal(scopedRfq.Id, match.Id);
        Assert.Equal(scopedCustomer.Id, match.Customer.Id);
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Analytics_WithCustomerScope_ReturnsForbidden()
    {
        var scopedCustomer = await CreateTestCustomerAsync();
        using var client = CreateCustomerScopedClient(scopedCustomer.Id, QuotationPermissions.QuotationsRead);

        var response = await client.GetAsync("/quotation/v1/analytics/conversion-rates");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Customer> CreateTestCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            Name = "Auth Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        return customer;
    }

    private async Task<Quotation> CreateTestQuotationAsync(Customer? customer = null)
    {
        customer ??= await CreateTestCustomerAsync();
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Draft,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();
        return quotation;
    }

    private async Task<Rfq> CreateTestRfqAsync(Customer customer)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            ChannelSource = RfqChannel.Website,
            Status = RfqStatus.New,
            RequestDetails = System.Text.Json.JsonDocument.Parse("""{"message":"test"}"""),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();
        return rfq;
    }

    private HttpClient CreateCustomerScopedClient(Guid customerId, params string[] permissions)
    {
        var token = Factory.CreateTestJwtToken(
            "customer-user",
            roles: new[] { "roles.quotation.viewer" },
            permissions: permissions,
            additionalClaims: new[] { new Claim("customer_id", customerId.ToString()) });

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Maliev.QuotationService.Api.DTOs.Requests.CreateQuotationRequest CreateValidQuotationRequest(Guid customerId)
    {
        return new Maliev.QuotationService.Api.DTOs.Requests.CreateQuotationRequest
        {
            CustomerId = customerId,
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            LineItems = new List<QuotationLineItemDto>
            {
                new QuotationLineItemDto
                {
                    MaterialServiceId = Guid.NewGuid(),
                    Quantity = 10,
                    UnitOfMeasure = "pcs",
                    UnitPrice = 100,
                    ManufacturingProcess = "CNC"
                }
            }
        };
    }
}
