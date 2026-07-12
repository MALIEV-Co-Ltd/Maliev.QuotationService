using Xunit;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;
using Maliev.QuotationService.Tests.Fixtures;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class QuotationStateManagementTests : BaseIntegrationTest
{
    private readonly Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>> _mockLogger;
    private readonly Maliev.QuotationService.Api.Services.QuotationService _quotationService;
    private readonly MetricsService _metricsService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;

    public QuotationStateManagementTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _mockLogger = new Mock<ILogger<Maliev.QuotationService.Api.Services.QuotationService>>();
        _metricsService = new MetricsService();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _quotationService = new Maliev.QuotationService.Api.Services.QuotationService(
            DbContext,
            _mockLogger.Object,
            _metricsService,
            _mockPublishEndpoint.Object,
            Mock.Of<IProjectServiceClient>());
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            Name = "Test Customer",
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

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _quotationService.UpdateStatusAsync(
            quotation.Id,
            QuotationStatus.PendingApproval,
            "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(QuotationStatus.PendingApproval, result.Status);

        // Verify audit log was created
        var auditLog = await DbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.EntityId == quotation.Id && a.ActionType == AuditActionType.Update);
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsException()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            Name = "Test Customer",
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

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _quotationService.UpdateStatusAsync(
                quotation.Id,
                QuotationStatus.Accepted, // Invalid: Can't go from Draft to Accepted
                "test-user");
        });
    }

    [Theory]
    [InlineData(QuotationStatus.Draft, QuotationStatus.PendingApproval, true)]
    [InlineData(QuotationStatus.Draft, QuotationStatus.Cancelled, true)]
    [InlineData(QuotationStatus.PendingApproval, QuotationStatus.Approved, true)]
    [InlineData(QuotationStatus.Approved, QuotationStatus.CustomerReview, true)]
    [InlineData(QuotationStatus.CustomerReview, QuotationStatus.Accepted, true)]
    [InlineData(QuotationStatus.Draft, QuotationStatus.Accepted, false)]
    [InlineData(QuotationStatus.Cancelled, QuotationStatus.Draft, false)]
    [InlineData(QuotationStatus.Accepted, QuotationStatus.Draft, false)]
    public void QuotationStateMachine_ValidateTransitions_ReturnsExpectedResult(
        QuotationStatus from,
        QuotationStatus to,
        bool expectedValid)
    {
        // Act
        var isValid = QuotationStateMachine.IsValidTransition(from, to);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }
}
