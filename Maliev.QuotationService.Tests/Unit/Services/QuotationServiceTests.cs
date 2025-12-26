using Xunit;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class QuotationServiceTests : IDisposable
{
    private readonly DbContextOptions<QuotationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>> _mockLogger;
    private readonly MetricsService _metricsService;

    public QuotationServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<QuotationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _mockLogger = new Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>>();
        _metricsService = new MetricsService();
    }

    public void Dispose()
    {
        _metricsService.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesQuotation()
    {
        // Arrange
        await using var context = new QuotationDbContext(_dbContextOptions);
        var service = new Maliev.QuotationService.Api.Services.QuotationService(context, _mockLogger.Object, _metricsService);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var lineItems = new List<object>
        {
            new { MaterialServiceId = Guid.NewGuid(), Quantity = 10, UnitPrice = 100.0m, ManufacturingProcess = "CNC" }
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

        var savedQuotation = await context.Quotations
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
        await using var context = new QuotationDbContext(_dbContextOptions);
        var service = new Maliev.QuotationService.Api.Services.QuotationService(context, _mockLogger.Object, _metricsService);

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

        await context.Customers.AddAsync(customer);
        await context.Rfqs.AddAsync(rfq);
        await context.SaveChangesAsync();

        var lineItems = new List<object>
        {
            new { MaterialServiceId = Guid.NewGuid(), Quantity = 5, UnitPrice = 200.0m, ManufacturingProcess = "Laser Cutting" }
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
        var updatedRfq = await context.Rfqs.FindAsync(rfq.Id);
        Assert.Equal(quotation.Id, updatedRfq!.ConvertedToQuotationId);
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewVersion()
    {
        // Arrange
        await using var context = new QuotationDbContext(_dbContextOptions);
        var service = new Maliev.QuotationService.Api.Services.QuotationService(context, _mockLogger.Object, _metricsService);

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

        quotation.CurrentVersionId = version1.Id;

        await context.Customers.AddAsync(customer);
        await context.Quotations.AddAsync(quotation);
        await context.QuotationVersions.AddAsync(version1);
        await context.SaveChangesAsync();

        var newLineItems = new List<object>
        {
            new { MaterialServiceId = Guid.NewGuid(), Quantity = 20, UnitPrice = 150.0m, ManufacturingProcess = "3D Printing" }
        };

        // Act
        var updatedQuotation = await service.UpdateAsync(
            quotationId: quotation.Id,
            lineItems: newLineItems,
            changeSummary: "Updated quantities and pricing",
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(updatedQuotation);
        Assert.NotEqual(version1.Id, updatedQuotation.CurrentVersionId);

        var savedQuotation = await context.Quotations
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == quotation.Id);

        Assert.Equal(2, savedQuotation!.Versions.Count);
        Assert.Contains(savedQuotation.Versions, v => v.VersionNumber == 2);
    }
}
