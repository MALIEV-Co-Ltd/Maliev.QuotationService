using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.QuotationService.Tests.Integration;

/// <summary>
/// Integration tests for dashboard metrics endpoints.
/// </summary>
public class MetricsControllerTests : BaseIntegrationTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">Integration test web application factory.</param>
    public MetricsControllerTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Verifies the default aging threshold counts only stale customer-review quotations.
    /// </summary>
    [Fact]
    public async Task GET_MetricsAgingCount_DefaultThreshold_ReturnsAgingCustomerReviewCount()
    {
        await CreateQuotationAsync(QuotationStatus.CustomerReview, DateTime.UtcNow.AddDays(-8));
        await CreateQuotationAsync(QuotationStatus.CustomerReview, DateTime.UtcNow.AddDays(-2));
        await CreateQuotationAsync(QuotationStatus.PendingApproval, DateTime.UtcNow.AddDays(-10));

        using var client = CreateAuthenticatedClient(roles: ["Viewer"]);

        var response = await client.GetAsync("/quotation/v1/metrics/aging-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MetricCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    /// <summary>
    /// Verifies a custom aging threshold is honored.
    /// </summary>
    [Fact]
    public async Task GET_MetricsAgingCount_CustomThreshold_ReturnsMatchingCount()
    {
        await CreateQuotationAsync(QuotationStatus.CustomerReview, DateTime.UtcNow.AddDays(-4));
        await CreateQuotationAsync(QuotationStatus.CustomerReview, DateTime.UtcNow.AddDays(-2));

        using var client = CreateAuthenticatedClient(roles: ["Viewer"]);

        var response = await client.GetAsync("/quotation/v1/metrics/aging-count?minAgeDays=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MetricCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    /// <summary>
    /// Verifies the endpoint requires quotation read permission.
    /// </summary>
    [Fact]
    public async Task GET_MetricsAgingCount_WithoutReadPermission_ReturnsForbidden()
    {
        using var client = Factory.CreateAuthenticatedClient(
            userId: "no-metrics-access",
            roles: [],
            permissions: []);

        var response = await client.GetAsync("/quotation/v1/metrics/aging-count");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task CreateQuotationAsync(QuotationStatus status, DateTime updatedAt)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            Name = "Metrics Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotationId = Guid.NewGuid();

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(new Quotation
        {
            Id = quotationId,
            CustomerId = customer.Id,
            Status = status,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        });

        await DbContext.SaveChangesAsync();

        await DbContext.Quotations
            .Where(quotation => quotation.Id == quotationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(quotation => quotation.CreatedAt, updatedAt)
                .SetProperty(quotation => quotation.UpdatedAt, updatedAt));
    }

    private sealed class MetricCountResponse
    {
        public int Count { get; set; }
    }
}
