using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
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
        var sourceProjectId = Guid.NewGuid();

        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        Factory.ProjectServiceClient.SetOwned(sourceProjectId, customer.Id, "PRJ-AUTHORITATIVE-001");

        var request = new CreateQuotationRequest
        {
            CustomerId = customer.Id,
            SourceProjectId = sourceProjectId,
            SourceProjectNumber = "PRJ-20260514-001",
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
            DeliveryExpectations = "2-3 weeks",
            ProjectSnapshotJson = $$"""{"projectId":"{{sourceProjectId}}","parts":[{"quantity":100}]}""",
            GeneratedByDisplayName = "Quoting Specialist",
            ChangeSummary = "Initial project quote snapshot"
        };

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/quotation/v1/quotations", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var quotationResponse = await response.Content.ReadFromJsonAsync<QuotationResponse>();
        Assert.NotNull(quotationResponse);
        Assert.NotEqual(Guid.Empty, quotationResponse.Id);
        Assert.Equal(customer.Id, quotationResponse.CustomerId);
        Assert.Equal(sourceProjectId, quotationResponse.SourceProjectId);
        Assert.Equal("PRJ-AUTHORITATIVE-001", quotationResponse.SourceProjectNumber);
        Assert.Equal(QuotationStatus.Draft, quotationResponse.Status);
        Assert.Equal(1, quotationResponse.CurrentVersionNumber);
        Assert.Single(quotationResponse.Versions);
        Assert.Equal("Initial project quote snapshot", quotationResponse.Versions[0].ChangeSummary);
        Assert.NotNull(quotationResponse.Versions[0].ProjectSnapshotHash);
        Assert.Equal("Quoting Specialist", quotationResponse.Versions[0].GeneratedByDisplayName);
        Assert.StartsWith("https://storage.example.test/", quotationResponse.Versions[0].PdfArtifactUrl);
        Assert.StartsWith("pdfs/quotation/", quotationResponse.Versions[0].PdfArtifactStoragePath);
        Assert.NotNull(quotationResponse.Versions[0].PdfGeneratedAt);

        // Verify database persistence
        var savedQuotation = await DbContext.Quotations
            .Include(q => q.Versions)
            .ThenInclude(v => v.LineItems)
            .FirstOrDefaultAsync(q => q.Id == quotationResponse.Id);

        Assert.NotNull(savedQuotation);
        Assert.Equal(sourceProjectId, savedQuotation.SourceProjectId);
        Assert.Equal("PRJ-AUTHORITATIVE-001", savedQuotation.SourceProjectNumber);
        Assert.Single(savedQuotation.Versions);
        Assert.Single(savedQuotation.Versions.First().LineItems);
        Assert.NotNull(savedQuotation.Versions.First().ProjectSnapshotJson);
        Assert.NotNull(savedQuotation.Versions.First().ProjectSnapshotHash);
        Assert.Equal(quotationResponse.Versions[0].PdfArtifactUrl, savedQuotation.Versions.First().PdfArtifactUrl);
        Assert.Equal(quotationResponse.Versions[0].PdfArtifactStoragePath, savedQuotation.Versions.First().PdfArtifactStoragePath);
        Assert.NotNull(savedQuotation.Versions.First().PdfGeneratedAt);

        var listResponse = await authenticatedClient.GetAsync($"/quotation/v1/quotations?customerId={customer.Id:D}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<QuotationResponse>>();
        Assert.NotNull(list);
        var listedQuotation = Assert.Single(list.Data, item => item.Id == quotationResponse.Id);
        var listedVersion = Assert.Single(listedQuotation.Versions);
        Assert.Equal(quotationResponse.Versions[0].Id, listedVersion.Id);
        Assert.Equal(quotationResponse.Versions[0].VersionNumber, listedVersion.VersionNumber);
        Assert.Equal(quotationResponse.Versions[0].PdfArtifactUrl, listedVersion.PdfArtifactUrl);
        Assert.Equal(quotationResponse.Versions[0].PdfArtifactStoragePath, listedVersion.PdfArtifactStoragePath);
        Assert.Equal(quotationResponse.Versions[0].ProjectSnapshotHash, listedVersion.ProjectSnapshotHash);
    }

    [Fact]
    public async Task AttachQuotationVersionPdfArtifact_ExistingVersion_PersistsArtifactOnExactVersion()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "pdf-version@example.com",
            Name = "PDF Version Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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

        var version = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            VersionNumber = 1,
            CreatedByUserId = "quote-user",
            TotalPrice = 1200m,
            CurrencyCode = "THB",
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        DbContext.QuotationVersions.Add(version);
        await DbContext.SaveChangesAsync();

        quotation.CurrentVersionId = version.Id;
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();
        var generatedAt = DateTime.UtcNow;

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id}/versions/1/pdf-artifact",
            new AttachQuotationVersionPdfRequest
            {
                PdfArtifactUrl = "https://files.maliev.com/quotation-v1.pdf",
                PdfArtifactStoragePath = "quotations/version-1.pdf",
                PdfGeneratedAt = generatedAt
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QuotationVersionResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.VersionNumber);
        Assert.Equal("https://files.maliev.com/quotation-v1.pdf", body.PdfArtifactUrl);
        Assert.Equal("quotations/version-1.pdf", body.PdfArtifactStoragePath);
        Assert.NotNull(body.PdfGeneratedAt);

        DbContext.Entry(version).State = EntityState.Detached;
        var savedVersion = await DbContext.QuotationVersions.FindAsync(version.Id);
        Assert.Equal("https://files.maliev.com/quotation-v1.pdf", savedVersion!.PdfArtifactUrl);
        Assert.Equal("quotations/version-1.pdf", savedVersion.PdfArtifactStoragePath);
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
            UpdatedAt = DateTime.UtcNow
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
        Assert.Equal(3, versions.Count);
        Assert.Collection(versions,
            item => Assert.Equal(3, item.VersionNumber),
            item => Assert.Equal(2, item.VersionNumber),
            item => Assert.Equal(1, item.VersionNumber)
        );

        var firstVersion = versions.First();
        Assert.Equal(3, firstVersion.VersionNumber);
        Assert.Equal("Final revision", firstVersion.ChangeSummary);
    }

    [Fact]
    public async Task GetQuotations_WithFilters_ReturnsPagedResults()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "paged@example.com",
            Name = "Paged Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/quotation/v1/quotations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<QuotationResponse>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Meta);
    }
}
