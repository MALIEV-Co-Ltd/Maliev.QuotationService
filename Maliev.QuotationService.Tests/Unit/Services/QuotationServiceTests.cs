using Xunit;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;
using Maliev.QuotationService.Tests.Fixtures;
using Maliev.QuotationService.Api.DTOs.Requests;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class QuotationServiceTests : BaseIntegrationTest
{
    private readonly Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>> _mockLogger;
    private readonly MetricsService _metricsService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;

    public QuotationServiceTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _mockLogger = new Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>>();
        _metricsService = new MetricsService();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesQuotation()
    {
        // Arrange
        var service = new Maliev.QuotationService.Api.Services.QuotationService(DbContext, _mockLogger.Object, _metricsService, _mockPublishEndpoint.Object);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Customers.AddAsync(customer);
        await DbContext.SaveChangesAsync();

        var lineItems = new List<QuotationLineItemDto>
        {
            new QuotationLineItemDto { MaterialServiceId = Guid.NewGuid(), Quantity = 10, UnitPrice = 100.0m, ManufacturingProcess = "CNC", UnitOfMeasure = "pcs" }
        };

        // Act
        var quotation = await service.CreateAsync(
            customerId: customer.Id,
            sourceRfqId: null,
            validityPeriodStart: DateTime.UtcNow,
            validityPeriodEnd: DateTime.UtcNow.AddDays(30),
            lineItems: lineItems,
            deliveryExpectations: "2 weeks",
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(quotation);
        Assert.Equal(customer.Id, quotation.CustomerId);
        Assert.Equal(QuotationStatus.Draft, quotation.Status);
        Assert.NotEqual(Guid.Empty, quotation.CurrentVersionId);

        var savedQuotation = await DbContext.Quotations
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == quotation.Id);

        Assert.NotNull(savedQuotation);
        Assert.Single(savedQuotation.Versions);
        Assert.Equal(1, savedQuotation.Versions.First().VersionNumber);
    }

    [Fact]
    public async Task CreateAsync_FromRfq_LinksToRfq()
    {
        // Arrange
        var service = new Maliev.QuotationService.Api.Services.QuotationService(DbContext, _mockLogger.Object, _metricsService, _mockPublishEndpoint.Object);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "rfq@example.com",
            Name = "RFQ Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ChannelSource = RfqChannel.Website,
            Status = RfqStatus.Qualified,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.Customers.AddAsync(customer);
        await DbContext.Rfqs.AddAsync(rfq);
        await DbContext.SaveChangesAsync();

        var lineItems = new List<QuotationLineItemDto>
        {
            new QuotationLineItemDto { MaterialServiceId = Guid.NewGuid(), Quantity = 5, UnitPrice = 200.0m, ManufacturingProcess = "Laser Cutting", UnitOfMeasure = "pcs" }
        };

        // Act
        var quotation = await service.CreateAsync(
            customerId: customer.Id,
            sourceRfqId: rfq.Id,
            validityPeriodStart: DateTime.UtcNow,
            validityPeriodEnd: DateTime.UtcNow.AddDays(30),
            lineItems: lineItems,
            deliveryExpectations: "3 weeks",
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(quotation);
        Assert.Equal(rfq.Id, quotation.SourceRfqId);
        Assert.Equal(customer.Id, quotation.CustomerId);

        // Verify RFQ status was updated
        var updatedRfq = await DbContext.Rfqs.FindAsync(rfq.Id);
        Assert.Equal(quotation.Id, updatedRfq!.ConvertedToQuotationId);
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewVersion()
    {
        // Arrange
        var service = new Maliev.QuotationService.Api.Services.QuotationService(DbContext, _mockLogger.Object, _metricsService, _mockPublishEndpoint.Object);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "version@example.com",
            Name = "Version Customer",
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

        await DbContext.Customers.AddAsync(customer);
        await DbContext.Quotations.AddAsync(quotation);
        await DbContext.SaveChangesAsync();

        var version1 = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            VersionNumber = 1,
            CreatedByUserId = "user1",
            TotalPrice = 1000.0m,
            CurrencyCode = "THB",
            CreatedAt = DateTime.UtcNow
        };

        await DbContext.QuotationVersions.AddAsync(version1);
        quotation.CurrentVersionId = version1.Id;
        await DbContext.SaveChangesAsync();

        var newLineItems = new List<QuotationLineItemDto>
        {
            new QuotationLineItemDto { MaterialServiceId = Guid.NewGuid(), Quantity = 20, UnitPrice = 150.0m, ManufacturingProcess = "3D Printing", UnitOfMeasure = "pcs" }
        };

        // Act
        var updatedQuotation = await service.UpdateAsync(
            quotationId: quotation.Id,
            lineItems: newLineItems,
            changeSummary: "Updated quantities and pricing",
            projectSnapshotJson: """{"projectNumber":"PRJ-002","parts":[{"quantity":20}]}""",
            generatedByDisplayName: "Natt Quoter",
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(updatedQuotation);
        Assert.NotEqual(version1.Id, updatedQuotation.CurrentVersionId);

        var savedQuotation = await DbContext.Quotations
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == quotation.Id);

        Assert.Equal(2, savedQuotation!.Versions.Count);
        Assert.Contains(savedQuotation.Versions, v => v.VersionNumber == 2);
        var secondVersion = savedQuotation.Versions.Single(v => v.VersionNumber == 2);
        Assert.NotNull(secondVersion.ProjectSnapshotJson);
        Assert.NotNull(secondVersion.ProjectSnapshotHash);
        Assert.Equal("Natt Quoter", secondVersion.GeneratedByDisplayName);
    }
}
