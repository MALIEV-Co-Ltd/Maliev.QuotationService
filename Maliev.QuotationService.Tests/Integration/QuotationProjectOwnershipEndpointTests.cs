using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public sealed class QuotationProjectOwnershipEndpointTests : BaseIntegrationTest
{
    public QuotationProjectOwnershipEndpointTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task POST_LinkedProjectOwnedByAnotherCustomer_ReturnsBareNotFoundWithoutWrites()
    {
        var customer = await AddCustomerAsync();
        var projectId = Guid.NewGuid();
        Factory.ProjectServiceClient.SetForeign();
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, projectId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(0, await DbContext.Quotations.CountAsync());
        var lookup = Assert.Single(Factory.ProjectServiceClient.Requests);
        Assert.Equal(projectId, lookup.ProjectId);
        Assert.Equal(customer.Id, lookup.CustomerId);
    }

    [Fact]
    public async Task POST_ProjectServiceUnavailable_ReturnsGenericProblemDetailsWithoutWrites()
    {
        var customer = await AddCustomerAsync();
        var projectId = Guid.NewGuid();
        Factory.ProjectServiceClient.SetUnavailable();
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, projectId));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Project service unavailable.", problem.Title);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, problem.Status);
        Assert.Null(problem.Detail);
        var problemBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(projectId.ToString(), problemBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("project-service", problemBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await DbContext.Quotations.CountAsync());
    }

    [Fact]
    public async Task PUT_LinkedProjectNoLongerOwned_ReturnsBareNotFoundWithoutRevision()
    {
        var quotation = await AddQuotationWithVersionAsync();
        var originalVersionId = quotation.CurrentVersionId;
        var versionsBefore = await DbContext.QuotationVersions.CountAsync();
        var auditsBefore = await DbContext.AuditLogEntries.CountAsync();
        Factory.ProjectServiceClient.SetMissing();
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            CreateUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(versionsBefore, await DbContext.QuotationVersions.CountAsync());
        Assert.Equal(auditsBefore, await DbContext.AuditLogEntries.CountAsync());
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalVersionId, unchanged.CurrentVersionId);
    }

    [Fact]
    public async Task PUT_ProjectServiceUnavailable_ReturnsGenericProblemDetailsWithoutRevision()
    {
        var quotation = await AddQuotationWithVersionAsync();
        var versionsBefore = await DbContext.QuotationVersions.CountAsync();
        Factory.ProjectServiceClient.SetUnavailable();
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            CreateUpdateRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Project service unavailable.", problem.Title);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, problem.Status);
        Assert.Null(problem.Detail);
        var problemBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(quotation.Id.ToString(), problemBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(quotation.SourceProjectId!.Value.ToString(), problemBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("project-service", problemBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(versionsBefore, await DbContext.QuotationVersions.CountAsync());
    }

    [Fact]
    public async Task PUT_OwnedProject_ValidatesPersistedCustomerProjectPairAndCreatesRevision()
    {
        var quotation = await AddQuotationWithVersionAsync();
        Factory.ProjectServiceClient.SetOwned(
            quotation.SourceProjectId!.Value,
            quotation.CustomerId,
            quotation.SourceProjectNumber!);
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            CreateUpdateRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lookup = Assert.Single(Factory.ProjectServiceClient.Requests);
        Assert.Equal(quotation.SourceProjectId, lookup.ProjectId);
        Assert.Equal(quotation.CustomerId, lookup.CustomerId);
        Assert.Equal(2, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
    }

    private async Task<Customer> AddCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            Name = "Endpoint Ownership Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        return customer;
    }

    private async Task<Quotation> AddQuotationWithVersionAsync()
    {
        var customer = await AddCustomerAsync();
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SourceProjectId = Guid.NewGuid(),
            SourceProjectNumber = "PRJ-PERSISTED-PAIR",
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
            CreatedByUserId = "test-user",
            TotalPrice = 100m,
            CurrencyCode = "THB",
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Quotations.Add(quotation);
        DbContext.QuotationVersions.Add(version);
        await DbContext.SaveChangesAsync();
        quotation.CurrentVersionId = version.Id;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return quotation;
    }

    private static CreateQuotationRequest CreateRequest(Guid customerId, Guid projectId) => new()
    {
        CustomerId = customerId,
        SourceProjectId = projectId,
        SourceProjectNumber = "UNTRUSTED",
        ValidityPeriodStart = DateTime.UtcNow,
        ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
        LineItems = CreateLineItems()
    };

    private static UpdateQuotationRequest CreateUpdateRequest() => new()
    {
        ChangeSummary = "Ownership-tested revision",
        LineItems = CreateLineItems()
    };

    private static List<QuotationLineItemDto> CreateLineItems() =>
    [
        new QuotationLineItemDto
        {
            MaterialServiceId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 100m,
            ManufacturingProcess = "CNC",
            UnitOfMeasure = "pcs"
        }
    ];
}
