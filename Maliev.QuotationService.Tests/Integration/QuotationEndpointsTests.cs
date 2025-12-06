using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public class QuotationEndpointsTests : BaseIntegrationTest
{
    public QuotationEndpointsTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateQuotation_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "quotation@example.com",
            Name = "Quotation Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        var request = new CreateQuotationRequest
        {
            CustomerId = customer.Id,
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            LineItems = new List<QuotationLineItemDto>
            {
                new QuotationLineItemDto
                {
                    MaterialServiceId = Guid.NewGuid(),
                    Quantity = 100,
                    UnitOfMeasure = "pieces",
                    UnitPrice = 50.0m,
                    ManufacturingProcess = "CNC Machining"
                }
            },
            DeliveryExpectations = "2-3 weeks"
        };

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/quotation/v1/quotations", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var quotationResponse = await response.Content.ReadFromJsonAsync<QuotationResponse>();
        Assert.NotNull(quotationResponse);
        Assert.NotEqual(Guid.Empty, quotationResponse!.Id);
        Assert.Equal(customer.Id, quotationResponse.CustomerId);
        Assert.Equal(QuotationStatus.Draft, quotationResponse.Status);
        Assert.Equal(1, quotationResponse.CurrentVersionNumber);

        // Verify database persistence
        var savedQuotation = await DbContext.Quotations
            .Include(q => q.Versions)
            .ThenInclude(v => v.LineItems)
            .FirstOrDefaultAsync(q => q.Id == quotationResponse.Id);

        Assert.NotNull(savedQuotation);
        Assert.Single(savedQuotation!.Versions);
        Assert.Single(savedQuotation.Versions.First().LineItems);
    }

    [Fact]
    public async Task GetVersions_MultipleVersions_ReturnsAllVersions()
    {
        // Arrange - Create quotation with multiple versions
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "versions@example.com",
            Name = "Versions Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Approved,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8] // Initialize with 8-byte array for concurrency
        };

        var version1 = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            VersionNumber = 1,
            CreatedByUserId = "user1",
            TotalPrice = 5000.0m,
            CurrencyCode = "THB",
            ChangeSummary = "Initial version",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var version2 = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            VersionNumber = 2,
            CreatedByUserId = "user2",
            TotalPrice = 4500.0m,
            CurrencyCode = "THB",
            ChangeSummary = "Price adjustment",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var version3 = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            VersionNumber = 3,
            CreatedByUserId = "user1",
            TotalPrice = 4700.0m,
            CurrencyCode = "THB",
            ChangeSummary = "Final revision",
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        DbContext.QuotationVersions.AddRange(version1, version2, version3);
        await DbContext.SaveChangesAsync();

        // Detach and reload to get fresh tracking state
        DbContext.Entry(quotation).State = EntityState.Detached;
        var reloadedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);

        // Update CurrentVersionId after saving to avoid circular dependency
        reloadedQuotation!.CurrentVersionId = version3.Id;
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync($"/quotation/v1/quotations/{quotation.Id}/versions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var versions = await response.Content.ReadFromJsonAsync<List<QuotationVersionResponse>>();
        Assert.NotNull(versions);
        Assert.Equal(3, versions!.Count);
        Assert.True(versions.SequenceEqual(versions.OrderByDescending(v => v.VersionNumber)));

        var firstVersion = versions[0];
        Assert.Equal(3, firstVersion.VersionNumber);
        Assert.Equal("Final revision", firstVersion.ChangeSummary);
    }
}
