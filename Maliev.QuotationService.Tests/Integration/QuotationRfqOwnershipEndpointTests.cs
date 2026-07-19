using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public sealed class QuotationRfqOwnershipEndpointTests : BaseIntegrationTest
{
    public QuotationRfqOwnershipEndpointTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task POST_ForeignRfqWithEmployeeToken_ReturnsBareNotFoundWithoutWrites()
    {
        var requestedCustomer = await AddCustomerAsync("requested");
        var rfqOwner = await AddCustomerAsync("rfq-owner");
        var rfq = await AddRfqAsync(rfqOwner, RfqStatus.Qualified);
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(requestedCustomer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(0, await DbContext.Quotations.CountAsync());
    }

    [Fact]
    public async Task POST_ForeignRfqWithMatchingCustomerScope_ReturnsBareNotFoundWithoutWrites()
    {
        var scopedCustomer = await AddCustomerAsync("scoped-requested");
        var rfqOwner = await AddCustomerAsync("scoped-foreign-owner");
        var rfq = await AddRfqAsync(rfqOwner, RfqStatus.Qualified);
        using var client = CreateCustomerScopedClient(
            scopedCustomer.Id,
            QuotationPermissions.QuotationsCreate);

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(scopedCustomer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(0, await DbContext.Quotations.CountAsync());
    }

    [Fact]
    public async Task POST_OwnedEligibleRfqWithMatchingCustomerScope_CreatesAndConverts()
    {
        var scopedCustomer = await AddCustomerAsync("scoped-owned");
        var rfq = await AddRfqAsync(scopedCustomer, RfqStatus.Qualified);
        using var client = CreateCustomerScopedClient(
            scopedCustomer.Id,
            QuotationPermissions.QuotationsCreate);

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(scopedCustomer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        DbContext.ChangeTracker.Clear();
        var quotation = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.SourceRfqId == rfq.Id);
        var converted = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(scopedCustomer.Id, quotation.CustomerId);
        Assert.Equal(RfqStatus.Converted, converted.Status);
        Assert.Equal(quotation.Id, converted.ConvertedToQuotationId);
    }

    [Fact]
    public async Task PUT_StoredForeignRfqWithEmployeeToken_ReturnsBareNotFoundWithoutRevision()
    {
        var quotationCustomer = await AddCustomerAsync("quotation-owner");
        var rfqOwner = await AddCustomerAsync("rfq-owner");
        var rfq = await AddRfqAsync(rfqOwner, RfqStatus.Converted);
        var quotation = await AddQuotationWithVersionAsync(quotationCustomer, rfq.Id);
        rfq.ConvertedToQuotationId = quotation.Id;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        var originalCurrentVersionId = quotation.CurrentVersionId;
        var versionsBefore = await DbContext.QuotationVersions.CountAsync();
        var auditsBefore = await DbContext.AuditLogEntries.CountAsync();
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            new UpdateQuotationRequest
            {
                ChangeSummary = "Denied foreign RFQ revision",
                LineItems = CreateLineItems()
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(versionsBefore, await DbContext.QuotationVersions.CountAsync());
        Assert.Equal(auditsBefore, await DbContext.AuditLogEntries.CountAsync());
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalCurrentVersionId, unchanged.CurrentVersionId);
    }

    [Theory]
    [InlineData(CreateDenialScenario.Missing)]
    [InlineData(CreateDenialScenario.Empty)]
    [InlineData(CreateDenialScenario.SoftDeleted)]
    [InlineData(CreateDenialScenario.New)]
    [InlineData(CreateDenialScenario.Abandoned)]
    [InlineData(CreateDenialScenario.AlreadyConverted)]
    [InlineData(CreateDenialScenario.EligibleWithQuotationPointer)]
    public async Task POST_IneligibleRfqWithEmployeeToken_ReturnsIdenticalBareNotFound(
        CreateDenialScenario scenario)
    {
        var customer = await AddCustomerAsync($"create-{scenario}");
        var rfqId = await ArrangeCreateDenialAsync(customer, scenario);
        DbContext.ChangeTracker.Clear();
        var quotationsBefore = await DbContext.Quotations.CountAsync();
        var versionsBefore = await DbContext.QuotationVersions.CountAsync();
        var auditsBefore = await DbContext.AuditLogEntries.CountAsync();
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, rfqId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(quotationsBefore, await DbContext.Quotations.CountAsync());
        Assert.Equal(versionsBefore, await DbContext.QuotationVersions.CountAsync());
        Assert.Equal(auditsBefore, await DbContext.AuditLogEntries.CountAsync());
    }

    [Theory]
    [InlineData(RevisionDenialScenario.Empty)]
    [InlineData(RevisionDenialScenario.SoftDeleted)]
    [InlineData(RevisionDenialScenario.WrongStatus)]
    [InlineData(RevisionDenialScenario.WrongQuotation)]
    public async Task PUT_InvalidStoredRfqWithEmployeeToken_ReturnsIdenticalBareNotFound(
        RevisionDenialScenario scenario)
    {
        var quotation = await ArrangeRevisionDenialAsync(scenario);
        var originalCurrentVersionId = quotation.CurrentVersionId;
        DbContext.ChangeTracker.Clear();
        var versionsBefore = await DbContext.QuotationVersions.CountAsync();
        var auditsBefore = await DbContext.AuditLogEntries.CountAsync();
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            new UpdateQuotationRequest
            {
                ChangeSummary = "Denied invalid RFQ revision",
                LineItems = CreateLineItems()
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(versionsBefore, await DbContext.QuotationVersions.CountAsync());
        Assert.Equal(auditsBefore, await DbContext.AuditLogEntries.CountAsync());
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalCurrentVersionId, unchanged.CurrentVersionId);
    }

    [Fact]
    public async Task POST_EligibleOwnedRfq_ConvertsAndLinksQuotation()
    {
        var customer = await AddCustomerAsync("owned-create");
        var rfq = await AddRfqAsync(customer, RfqStatus.Qualified);
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        DbContext.ChangeTracker.Clear();
        var quotation = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.SourceRfqId == rfq.Id);
        var converted = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, converted.Status);
        Assert.Equal(quotation.Id, converted.ConvertedToQuotationId);
        Assert.Equal(1, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
    }

    [Fact]
    public async Task POST_ConvertThenCreate_PublicTwoStepFlow_CreatesAndLinksQuotation()
    {
        var customer = await AddCustomerAsync("two-step");
        var rfq = await AddRfqAsync(customer, RfqStatus.Qualified);
        using var client = CreateCustomerScopedClient(
            customer.Id,
            QuotationPermissions.QuotationsCreate,
            QuotationPermissions.QuotationsUpdate);

        var convertResponse = await client.PostAsync(
            $"/quotation/v1/rfqs/{rfq.Id:D}/convert",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, convertResponse.StatusCode);
        DbContext.ChangeTracker.Clear();
        var convertedWithoutQuotation = await DbContext.Rfqs
            .AsNoTracking()
            .SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, convertedWithoutQuotation.Status);
        Assert.Null(convertedWithoutQuotation.ConvertedToQuotationId);

        var createResponse = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        DbContext.ChangeTracker.Clear();
        var quotation = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.SourceRfqId == rfq.Id);
        var linkedRfq = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, linkedRfq.Status);
        Assert.Equal(quotation.Id, linkedRfq.ConvertedToQuotationId);
    }

    [Theory]
    [InlineData(RfqStatus.New)]
    [InlineData(RfqStatus.Abandoned)]
    public async Task POST_ConvertInvalidLifecycle_ReturnsConflictThenQuotationCreateReturnsBareNotFound(
        RfqStatus status)
    {
        var customer = await AddCustomerAsync($"convert-conflict-{status}");
        var rfq = await AddRfqAsync(customer, status);
        DbContext.ChangeTracker.Clear();
        var originalUpdatedAt = await DbContext.Rfqs
            .AsNoTracking()
            .Where(item => item.Id == rfq.Id)
            .Select(item => item.UpdatedAt)
            .SingleAsync();
        using var client = CreateCustomerScopedClient(
            customer.Id,
            QuotationPermissions.QuotationsCreate,
            QuotationPermissions.QuotationsUpdate);

        var convertResponse = await client.PostAsync(
            $"/quotation/v1/rfqs/{rfq.Id:D}/convert",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, convertResponse.StatusCode);
        var problem = await convertResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("RFQ cannot be converted.", problem.Title);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Null(problem.Detail);
        var conflictBody = await convertResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rfq.Id.ToString(), conflictBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(status.ToString(), conflictBody, StringComparison.OrdinalIgnoreCase);

        var createResponse = await client.PostAsJsonAsync(
            "/quotation/v1/quotations",
            CreateRequest(customer.Id, rfq.Id));

        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
        Assert.Empty(await createResponse.Content.ReadAsByteArrayAsync());
        Assert.Equal(0, await DbContext.Quotations.CountAsync());
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(status, unchanged.Status);
        Assert.Null(unchanged.ConvertedToQuotationId);
        Assert.Equal(originalUpdatedAt, unchanged.UpdatedAt);
    }

    [Fact]
    public async Task PUT_OwnedConvertedRfq_CreatesExactlyVersionTwo()
    {
        var customer = await AddCustomerAsync("owned-revision");
        var rfq = await AddRfqAsync(customer, RfqStatus.Converted);
        var quotation = await AddQuotationWithVersionAsync(customer, rfq.Id);
        rfq.ConvertedToQuotationId = quotation.Id;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id:D}",
            new UpdateQuotationRequest
            {
                ChangeSummary = "Owned RFQ revision",
                LineItems = CreateLineItems()
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var versions = await DbContext.QuotationVersions
            .AsNoTracking()
            .Where(item => item.QuotationId == quotation.Id)
            .OrderBy(item => item.VersionNumber)
            .ToListAsync();
        Assert.Collection(
            versions,
            first => Assert.Equal(1, first.VersionNumber),
            second => Assert.Equal(2, second.VersionNumber));
    }

    [Fact]
    public async Task POST_NoSourceRfq_RemainsCompatible()
    {
        var customer = await AddCustomerAsync("unlinked");
        using var client = CreateAuthenticatedClient();
        var request = CreateRequest(customer.Id, Guid.NewGuid());
        request.SourceRfqId = null;

        var response = await client.PostAsJsonAsync("/quotation/v1/quotations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, await DbContext.Quotations.CountAsync(item => item.SourceRfqId == null));
    }

    private async Task<Customer> AddCustomerAsync(string prefix)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
            Name = $"{prefix} customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        return customer;
    }

    private async Task<Rfq> AddRfqAsync(Customer customer, RfqStatus status)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ChannelSource = RfqChannel.Website,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();
        return rfq;
    }

    private async Task<Quotation> AddQuotationWithVersionAsync(Customer customer, Guid? sourceRfqId)
    {
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SourceRfqId = sourceRfqId,
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
        return quotation;
    }

    private async Task<Guid> ArrangeCreateDenialAsync(Customer customer, CreateDenialScenario scenario)
    {
        if (scenario == CreateDenialScenario.Missing)
        {
            return Guid.NewGuid();
        }

        if (scenario == CreateDenialScenario.Empty)
        {
            return Guid.Empty;
        }

        var status = scenario switch
        {
            CreateDenialScenario.New => RfqStatus.New,
            CreateDenialScenario.Abandoned => RfqStatus.Abandoned,
            _ => RfqStatus.Qualified
        };
        var rfq = await AddRfqAsync(customer, status);
        if (scenario == CreateDenialScenario.SoftDeleted)
        {
            rfq.IsDeleted = true;
            rfq.DeletedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();
        }
        else if (scenario is CreateDenialScenario.AlreadyConverted or CreateDenialScenario.EligibleWithQuotationPointer)
        {
            var existingQuotation = await AddQuotationWithVersionAsync(customer, sourceRfqId: null);
            rfq.Status = scenario == CreateDenialScenario.AlreadyConverted
                ? RfqStatus.Converted
                : RfqStatus.Qualified;
            rfq.ConvertedToQuotationId = existingQuotation.Id;
            await DbContext.SaveChangesAsync();
        }

        return rfq.Id;
    }

    private async Task<Quotation> ArrangeRevisionDenialAsync(RevisionDenialScenario scenario)
    {
        var customer = await AddCustomerAsync($"revision-{scenario}");
        if (scenario == RevisionDenialScenario.Empty)
        {
            await InsertEmptyRfqAsync(customer.Id);
            var emptyQuotation = await AddQuotationWithVersionAsync(customer, Guid.Empty);
            await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE rfqs SET status = {(int)RfqStatus.Converted}, converted_to_quotation_id = {emptyQuotation.Id} WHERE id = {Guid.Empty}");
            return emptyQuotation;
        }

        var initialStatus = scenario == RevisionDenialScenario.WrongStatus
            ? RfqStatus.Qualified
            : RfqStatus.Converted;
        var rfq = await AddRfqAsync(customer, initialStatus);
        var quotation = await AddQuotationWithVersionAsync(customer, rfq.Id);
        if (scenario == RevisionDenialScenario.WrongQuotation)
        {
            var otherQuotation = await AddQuotationWithVersionAsync(customer, sourceRfqId: null);
            rfq.ConvertedToQuotationId = otherQuotation.Id;
        }
        else
        {
            rfq.ConvertedToQuotationId = quotation.Id;
        }

        if (scenario == RevisionDenialScenario.SoftDeleted)
        {
            rfq.IsDeleted = true;
            rfq.DeletedAt = DateTime.UtcNow;
        }
        await DbContext.SaveChangesAsync();
        return quotation;
    }

    private Task InsertEmptyRfqAsync(Guid customerId) =>
        DbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO rfqs (id, customer_id, channel_source, status, created_at, updated_at, is_deleted)
            VALUES ({Guid.Empty}, {customerId}, {(int)RfqChannel.Website}, {(int)RfqStatus.Converted}, {DateTime.UtcNow}, {DateTime.UtcNow}, FALSE)
            """);

    private static CreateQuotationRequest CreateRequest(Guid customerId, Guid rfqId) => new()
    {
        CustomerId = customerId,
        SourceRfqId = rfqId,
        ValidityPeriodStart = DateTime.UtcNow,
        ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
        LineItems = CreateLineItems()
    };

    private HttpClient CreateCustomerScopedClient(Guid customerId, params string[] permissions)
    {
        var token = Factory.CreateTestJwtToken(
            "scoped-customer-user",
            roles: ["roles.quotation.viewer"],
            permissions: permissions,
            additionalClaims: [new Claim("customer_id", customerId.ToString())]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

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

    public enum CreateDenialScenario
    {
        Missing,
        Empty,
        SoftDeleted,
        New,
        Abandoned,
        AlreadyConverted,
        EligibleWithQuotationPointer
    }

    public enum RevisionDenialScenario
    {
        Empty,
        SoftDeleted,
        WrongStatus,
        WrongQuotation
    }
}
