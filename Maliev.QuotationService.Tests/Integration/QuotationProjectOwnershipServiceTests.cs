using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.QuotationService.Tests.Integration;

public sealed class QuotationProjectOwnershipServiceTests : BaseIntegrationTest
{
    private readonly Mock<ILogger<Api.Services.QuotationService>> _logger = new();
    private readonly MetricsService _metrics = new();

    public QuotationProjectOwnershipServiceTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateAsync_OwnedProject_PersistsAuthoritativeProjectNumber()
    {
        var customer = await AddCustomerAsync();
        var projectId = Guid.NewGuid();
        var projectClient = ProjectClientReturning(ProjectOwnershipStatus.Owned, "PRJ-AUTHORITATIVE");
        var service = CreateService(projectClient.Object);

        var quotation = await service.CreateAsync(
            customer.Id,
            sourceRfqId: null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user",
            sourceProjectId: projectId,
            sourceProjectNumber: "UNTRUSTED-CALLER-VALUE");

        Assert.Equal(projectId, quotation.SourceProjectId);
        Assert.Equal("PRJ-AUTHORITATIVE", quotation.SourceProjectNumber);
        projectClient.Verify(
            client => client.VerifyOwnershipAsync(projectId, customer.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ProjectOwnershipStatus.NotOwned)]
    [InlineData(ProjectOwnershipStatus.Unavailable)]
    public async Task CreateAsync_ProjectNotTrusted_FailsBeforeAnySideEffect(ProjectOwnershipStatus status)
    {
        var customerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var projectClient = ProjectClientReturning(status);
        var customerClient = new Mock<ICustomerServiceClient>();
        var pdfClient = new Mock<IPdfServiceClient>();
        var publisher = new Mock<IPublishEndpoint>();
        var service = CreateService(projectClient.Object, customerClient.Object, pdfClient.Object, publisher.Object);
        var before = await CaptureEffectsAsync();

        Exception exception = status == ProjectOwnershipStatus.NotOwned
            ? await Assert.ThrowsAsync<ProjectNotFoundException>(() => service.CreateAsync(
                customerId,
                sourceRfqId: null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                CreateLineItems(),
                deliveryExpectations: null,
                currentUserId: "test-user",
                sourceProjectId: projectId))
            : await Assert.ThrowsAsync<ProjectServiceUnavailableException>(() => service.CreateAsync(
                customerId,
                sourceRfqId: null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                CreateLineItems(),
                deliveryExpectations: null,
                currentUserId: "test-user",
                sourceProjectId: projectId));

        if (status == ProjectOwnershipStatus.NotOwned)
        {
            Assert.Equal($"Project with ID {projectId} not found", exception.Message);
        }

        await AssertEffectsUnchangedAsync(before);
        Assert.False(DbContext.ChangeTracker.HasChanges());
        customerClient.Verify(
            client => client.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        pdfClient.Verify(
            client => client.GeneratePdfAsync(It.IsAny<QuotationPdfPayload>(), It.IsAny<CancellationToken>()),
            Times.Never);
        publisher.Verify(
            endpoint => endpoint.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(customerClient.Invocations);
        Assert.Empty(pdfClient.Invocations);
        Assert.Empty(publisher.Invocations);
    }

    [Fact]
    public async Task CreateAsync_NoSourceProject_SkipsProjectService()
    {
        var customer = await AddCustomerAsync();
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(projectClient.Object);

        var quotation = await service.CreateAsync(
            customer.Id,
            sourceRfqId: null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user");

        Assert.Null(quotation.SourceProjectId);
        projectClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_EmptySourceProject_ReturnsSameNotFoundWithoutCallingProjectService()
    {
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(projectClient.Object);
        var before = await CaptureEffectsAsync();

        var exception = await Assert.ThrowsAsync<ProjectNotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(),
            sourceRfqId: null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user",
            sourceProjectId: Guid.Empty));

        Assert.Equal("Project with ID 00000000-0000-0000-0000-000000000000 not found", exception.Message);
        projectClient.VerifyNoOtherCalls();
        await AssertEffectsUnchangedAsync(before);
    }

    [Theory]
    [InlineData(ProjectOwnershipStatus.NotOwned)]
    [InlineData(ProjectOwnershipStatus.Unavailable)]
    public async Task UpdateAsync_ProjectNotTrusted_DoesNotCreateVersionAuditOrMutation(ProjectOwnershipStatus status)
    {
        var projectId = Guid.NewGuid();
        var quotation = await AddQuotationWithVersionAsync(projectId);
        var originalCurrentVersionId = quotation.CurrentVersionId;
        DbContext.ChangeTracker.Clear();
        var projectClient = ProjectClientReturning(status);
        var pdfClient = new Mock<IPdfServiceClient>();
        var publisher = new Mock<IPublishEndpoint>();
        var service = CreateService(projectClient.Object, pdfClient: pdfClient.Object, publisher: publisher.Object);
        var before = await CaptureEffectsAsync();

        if (status == ProjectOwnershipStatus.NotOwned)
        {
            var exception = await Assert.ThrowsAsync<ProjectNotFoundException>(() => service.UpdateAsync(
                quotation.Id,
                CreateLineItems(),
                "Denied revision",
                currentUserId: "test-user"));
            Assert.Equal($"Project with ID {projectId} not found", exception.Message);
        }
        else
        {
            await Assert.ThrowsAsync<ProjectServiceUnavailableException>(() => service.UpdateAsync(
                quotation.Id,
                CreateLineItems(),
                "Denied revision",
                currentUserId: "test-user"));
        }

        await AssertEffectsUnchangedAsync(before);
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalCurrentVersionId, unchanged.CurrentVersionId);
        pdfClient.Verify(
            client => client.GeneratePdfAsync(It.IsAny<QuotationPdfPayload>(), It.IsAny<CancellationToken>()),
            Times.Never);
        publisher.Verify(
            endpoint => endpoint.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(pdfClient.Invocations);
        Assert.Empty(publisher.Invocations);
    }

    [Fact]
    public async Task UpdateAsync_OwnedPersistedProject_CreatesRevision()
    {
        var projectId = Guid.NewGuid();
        var quotation = await AddQuotationWithVersionAsync(projectId);
        DbContext.ChangeTracker.Clear();
        var projectClient = ProjectClientReturning(ProjectOwnershipStatus.Owned, quotation.SourceProjectNumber);
        var service = CreateService(projectClient.Object);

        await service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Owned revision",
            currentUserId: "test-user");

        Assert.Equal(2, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
        projectClient.Verify(
            client => client.VerifyOwnershipAsync(projectId, quotation.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NoSourceProject_SkipsProjectService()
    {
        var quotation = await AddQuotationWithVersionAsync(sourceProjectId: null);
        DbContext.ChangeTracker.Clear();
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(projectClient.Object);

        await service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Unlinked revision",
            currentUserId: "test-user");

        Assert.Equal(2, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
        projectClient.VerifyNoOtherCalls();
    }

    private Api.Services.QuotationService CreateService(
        IProjectServiceClient projectClient,
        ICustomerServiceClient? customerClient = null,
        IPdfServiceClient? pdfClient = null,
        IPublishEndpoint? publisher = null) =>
        new(
            DbContext,
            _logger.Object,
            _metrics,
            publisher ?? Mock.Of<IPublishEndpoint>(),
            projectClient,
            customerClient,
            pdfClient);

    private static Mock<IProjectServiceClient> ProjectClientReturning(
        ProjectOwnershipStatus status,
        string? projectNumber = null)
    {
        var mock = new Mock<IProjectServiceClient>();
        mock.Setup(client => client.VerifyOwnershipAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectOwnershipResult(status, projectNumber));
        return mock;
    }

    private async Task<Customer> AddCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            Name = "Ownership Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        return customer;
    }

    private async Task<Quotation> AddQuotationWithVersionAsync(Guid? sourceProjectId)
    {
        var customer = await AddCustomerAsync();
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SourceProjectId = sourceProjectId,
            SourceProjectNumber = sourceProjectId.HasValue ? "PRJ-PERSISTED" : null,
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

    private static List<QuotationLineItemDto> CreateLineItems() =>
    [
        new QuotationLineItemDto
        {
            MaterialServiceId = Guid.NewGuid(),
            Quantity = 2,
            UnitPrice = 50m,
            ManufacturingProcess = "CNC",
            UnitOfMeasure = "pcs"
        }
    ];

    private async Task<EffectCounts> CaptureEffectsAsync() => new(
        await DbContext.Customers.CountAsync(),
        await DbContext.Quotations.CountAsync(),
        await DbContext.QuotationVersions.CountAsync(),
        await DbContext.QuotationLineItems.CountAsync(),
        await DbContext.DiscountStructures.CountAsync(),
        await DbContext.AuditLogEntries.CountAsync(),
        await CountOutboxMessagesAsync());

    private async Task AssertEffectsUnchangedAsync(EffectCounts before)
    {
        Assert.Equal(before, await CaptureEffectsAsync());
    }

    private Task<int> CountOutboxMessagesAsync() =>
        DbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM outbox_message")
            .SingleAsync();

    private sealed record EffectCounts(
        int Customers,
        int Quotations,
        int Versions,
        int LineItems,
        int Discounts,
        int Audits,
        int OutboxMessages);
}
