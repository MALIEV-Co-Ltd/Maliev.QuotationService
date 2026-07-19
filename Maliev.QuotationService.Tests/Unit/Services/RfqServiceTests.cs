using Xunit;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Exceptions;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Api.Services.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.QuotationService.Tests.Fixtures;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class RfqServiceTests : BaseIntegrationTest
{
    private readonly Mock<ILogger<RfqService>> _mockLogger;
    private readonly MetricsService _metricsService;

    public RfqServiceTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _mockLogger = new Mock<ILogger<RfqService>>();
        _metricsService = new MetricsService();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesRfq()
    {
        // Arrange
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);

        // Act
        var rfq = await service.CreateAsync(
            customerEmail: "test@example.com",
            customerName: "Test Customer",
            customerPhoneNumber: "+66123456789",
            channelSource: RfqChannel.Website,
            requestDetails: new { description = "Test request" },
            uploadServiceFileIds: null,
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(rfq);
        Assert.Equal(RfqChannel.Website, rfq.ChannelSource);
        Assert.Equal(RfqStatus.New, rfq.Status);
        Assert.True(DateTime.UtcNow.Subtract(rfq.CreatedAt).TotalSeconds < 5);

        var savedRfq = await DbContext.Rfqs.Include(r => r.Customer).FirstOrDefaultAsync(r => r.Id == rfq.Id);
        Assert.NotNull(savedRfq);
        Assert.Equal("test@example.com", savedRfq.Customer.Email);
    }

    [Fact]
    public async Task UpdateAsync_ValidRfq_UpdatesDetails()
    {
        // Arrange
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ChannelSource = RfqChannel.Website,
            Status = RfqStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();

        // Capture original UpdatedAt before update
        var originalUpdatedAt = rfq.UpdatedAt;

        // Small delay to ensure UpdatedAt timestamp will be different
        await Task.Delay(10);

        var newRequestDetails = new { description = "Updated request details" };

        // Act
        var updated = await service.UpdateAsync(
            rfqId: rfq.Id,
            requestDetails: newRequestDetails,
            assignedStaffUserId: "staff-123",
            currentUserId: "test-user"
        );

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("staff-123", updated.AssignedStaffUserId);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);

        var savedRfq = await DbContext.Rfqs.FindAsync(rfq.Id);
        Assert.Equal("staff-123", savedRfq!.AssignedStaffUserId);
    }

    [Fact]
    public async Task AssignAsync_ValidStaffUser_AssignsRfq()
    {
        // Arrange
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ChannelSource = RfqChannel.Email,
            Status = RfqStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();

        var staffUserId = "staff-456";

        // Act
        var assigned = await service.AssignAsync(
            rfqId: rfq.Id,
            assignedStaffUserId: staffUserId,
            currentUserId: "manager-789"
        );

        // Assert
        Assert.NotNull(assigned);
        Assert.Equal(staffUserId, assigned.AssignedStaffUserId);

        var savedRfq = await DbContext.Rfqs.FindAsync(rfq.Id);
        Assert.Equal(staffUserId, savedRfq!.AssignedStaffUserId);

        // Verify audit log entry was created
        var auditEntry = await DbContext.AuditLogEntries
            .Where(a => a.EntityType == AuditEntityType.RFQ && a.EntityId == rfq.Id)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditEntry);
        Assert.Equal(AuditActionType.Update, auditEntry.ActionType);
        Assert.Equal("manager-789", auditEntry.UserId);
    }

    [Theory]
    [InlineData(RfqStatus.InProgress)]
    [InlineData(RfqStatus.Qualified)]
    public async Task MarkRfqAsConvertedAsync_EligibleUnclaimedRfq_TransitionsToConverted(
        RfqStatus initialStatus)
    {
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);
        var customer = await AddConversionCustomerAsync();
        var rfq = await AddConversionRfqAsync(customer.Id, initialStatus);

        var result = await service.MarkRfqAsConvertedAsync(rfq.Id, "test-user");

        Assert.Equal(rfq.Id, result);
        DbContext.ChangeTracker.Clear();
        var converted = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, converted.Status);
        Assert.Null(converted.ConvertedToQuotationId);
    }

    [Fact]
    public async Task MarkRfqAsConvertedAsync_ConvertedWithoutQuotation_IsIdempotent()
    {
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);
        var customer = await AddConversionCustomerAsync();
        var rfq = await AddConversionRfqAsync(customer.Id, RfqStatus.Converted);
        DbContext.ChangeTracker.Clear();
        var before = await CaptureConversionEffectsAsync(rfq.Id);

        var result = await service.MarkRfqAsConvertedAsync(rfq.Id, "test-user");

        Assert.Equal(rfq.Id, result);
        Assert.Equal(before, await CaptureConversionEffectsAsync(rfq.Id));
    }

    [Theory]
    [InlineData(ConversionConflictScenario.New)]
    [InlineData(ConversionConflictScenario.Abandoned)]
    [InlineData(ConversionConflictScenario.ClaimedConverted)]
    public async Task MarkRfqAsConvertedAsync_InvalidLifecycle_RejectsWithoutMutation(
        ConversionConflictScenario scenario)
    {
        var service = new RfqService(DbContext, _mockLogger.Object, _metricsService);
        var customer = await AddConversionCustomerAsync();
        var status = scenario switch
        {
            ConversionConflictScenario.New => RfqStatus.New,
            ConversionConflictScenario.Abandoned => RfqStatus.Abandoned,
            _ => RfqStatus.Converted
        };
        var rfq = await AddConversionRfqAsync(customer.Id, status);
        if (scenario == ConversionConflictScenario.ClaimedConverted)
        {
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
            rfq.ConvertedToQuotationId = quotation.Id;
            await DbContext.SaveChangesAsync();
        }

        DbContext.ChangeTracker.Clear();
        var before = await CaptureConversionEffectsAsync(rfq.Id);

        await Assert.ThrowsAsync<RfqConversionConflictException>(() =>
            service.MarkRfqAsConvertedAsync(rfq.Id, "test-user"));

        Assert.Equal(before, await CaptureConversionEffectsAsync(rfq.Id));
    }

    private async Task<Customer> AddConversionCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"conversion-{Guid.NewGuid():N}@example.test",
            Name = "Conversion Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        return customer;
    }

    private async Task<Rfq> AddConversionRfqAsync(Guid customerId, RfqStatus status)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ChannelSource = RfqChannel.Website,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();
        return rfq;
    }

    private async Task<ConversionEffects> CaptureConversionEffectsAsync(Guid rfqId)
    {
        DbContext.ChangeTracker.Clear();
        var rfq = await DbContext.Rfqs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(item => item.Id == rfqId);
        return new ConversionEffects(
            rfq.Status,
            rfq.ConvertedToQuotationId,
            rfq.UpdatedAt,
            await DbContext.AuditLogEntries.CountAsync(),
            await DbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM outbox_message").SingleAsync());
    }

    public enum ConversionConflictScenario
    {
        New,
        Abandoned,
        ClaimedConverted
    }

    private sealed record ConversionEffects(
        RfqStatus Status,
        Guid? ConvertedToQuotationId,
        DateTime UpdatedAt,
        int Audits,
        int OutboxMessages);
}
