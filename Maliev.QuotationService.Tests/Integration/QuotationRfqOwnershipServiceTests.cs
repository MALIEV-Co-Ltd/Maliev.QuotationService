using System.Data.Common;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.Exceptions;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Tests.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.QuotationService.Tests.Integration;

public sealed class QuotationRfqOwnershipServiceTests : BaseIntegrationTest
{
    private readonly Mock<ILogger<Api.Services.QuotationService>> _logger = new();
    private readonly MetricsService _metrics = new();

    public QuotationRfqOwnershipServiceTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateAsync_ForeignRfq_FailsBeforeProjectLookupHydrationOrWrites()
    {
        var rfqOwner = await AddCustomerAsync("rfq-owner");
        var rfq = await AddRfqAsync(rfqOwner, RfqStatus.Qualified);
        var requestedCustomerId = Guid.NewGuid();
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var customerClient = new Mock<ICustomerServiceClient>(MockBehavior.Strict);
        var pdfClient = new Mock<IPdfServiceClient>(MockBehavior.Strict);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object, customerClient.Object, pdfClient.Object, publisher.Object);
        DbContext.ChangeTracker.Clear();
        var before = await CaptureEffectsAsync(DbContext);

        var exception = await Assert.ThrowsAsync<RfqNotFoundException>(() => service.CreateAsync(
            requestedCustomerId,
            rfq.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user",
            sourceProjectId: Guid.NewGuid()));

        Assert.Equal($"RFQ with ID {rfq.Id} not found", exception.Message);
        Assert.Equal(before, await CaptureEffectsAsync(DbContext));
        Assert.False(DbContext.ChangeTracker.HasChanges());
        projectClient.VerifyNoOtherCalls();
        customerClient.VerifyNoOtherCalls();
        pdfClient.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_StoredForeignRfq_DoesNotCreateVersionAuditOrMutation()
    {
        var quotationCustomer = await AddCustomerAsync("quotation-owner");
        var rfqOwner = await AddCustomerAsync("rfq-owner");
        var rfq = await AddRfqAsync(rfqOwner, RfqStatus.Converted);
        var quotation = await AddQuotationWithVersionAsync(quotationCustomer, rfq.Id);
        rfq.ConvertedToQuotationId = quotation.Id;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        var originalCurrentVersionId = quotation.CurrentVersionId;
        var before = await CaptureEffectsAsync(DbContext);
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object);

        var exception = await Assert.ThrowsAsync<RfqNotFoundException>(() => service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Denied foreign RFQ revision",
            currentUserId: "test-user"));

        Assert.Equal($"RFQ with ID {rfq.Id} not found", exception.Message);
        Assert.Equal(before, await CaptureEffectsAsync(DbContext));
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalCurrentVersionId, unchanged.CurrentVersionId);
        projectClient.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(CreateDenialScenario.Missing)]
    [InlineData(CreateDenialScenario.Empty)]
    [InlineData(CreateDenialScenario.SoftDeleted)]
    [InlineData(CreateDenialScenario.New)]
    [InlineData(CreateDenialScenario.Abandoned)]
    [InlineData(CreateDenialScenario.AlreadyConverted)]
    [InlineData(CreateDenialScenario.EligibleWithQuotationPointer)]
    public async Task CreateAsync_IneligibleRfq_ReturnsSameNotFoundBeforeAnySideEffect(
        CreateDenialScenario scenario)
    {
        var (customerId, rfqId) = await ArrangeCreateDenialAsync(scenario);
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var customerClient = new Mock<ICustomerServiceClient>(MockBehavior.Strict);
        var pdfClient = new Mock<IPdfServiceClient>(MockBehavior.Strict);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object, customerClient.Object, pdfClient.Object, publisher.Object);
        DbContext.ChangeTracker.Clear();
        var before = await CaptureEffectsAsync(DbContext);

        var exception = await Assert.ThrowsAsync<RfqNotFoundException>(() => service.CreateAsync(
            customerId,
            rfqId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user",
            sourceProjectId: Guid.NewGuid()));

        Assert.Equal($"RFQ with ID {rfqId} not found", exception.Message);
        Assert.Equal(before, await CaptureEffectsAsync(DbContext));
        Assert.False(DbContext.ChangeTracker.HasChanges());
        projectClient.VerifyNoOtherCalls();
        customerClient.VerifyNoOtherCalls();
        pdfClient.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(RfqStatus.Qualified)]
    [InlineData(RfqStatus.InProgress)]
    [InlineData(RfqStatus.Converted)]
    public async Task CreateAsync_EligibleOwnedRfq_AtomicallyConvertsAndLinksQuotation(RfqStatus status)
    {
        var customer = await AddCustomerAsync("owned");
        var rfq = await AddRfqAsync(customer, status);
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var customerClient = new Mock<ICustomerServiceClient>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object, customerClient.Object);

        var quotation = await service.CreateAsync(
            customer.Id,
            rfq.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user");

        DbContext.ChangeTracker.Clear();
        var converted = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, converted.Status);
        Assert.Equal(quotation.Id, converted.ConvertedToQuotationId);
        Assert.Equal(rfq.Id, quotation.SourceRfqId);
        Assert.Equal(1, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
        projectClient.VerifyNoOtherCalls();
        customerClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_NoSourceRfq_RemainsCompatible()
    {
        var customer = await AddCustomerAsync("unlinked");
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var customerClient = new Mock<ICustomerServiceClient>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object, customerClient.Object);

        var quotation = await service.CreateAsync(
            customer.Id,
            sourceRfqId: null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            CreateLineItems(),
            deliveryExpectations: null,
            currentUserId: "test-user");

        Assert.Null(quotation.SourceRfqId);
        projectClient.VerifyNoOtherCalls();
        customerClient.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(RevisionDenialScenario.Empty)]
    [InlineData(RevisionDenialScenario.SoftDeleted)]
    [InlineData(RevisionDenialScenario.WrongStatus)]
    [InlineData(RevisionDenialScenario.WrongQuotation)]
    public async Task UpdateAsync_InvalidStoredRfq_ReturnsSameNotFoundWithoutRevision(
        RevisionDenialScenario scenario)
    {
        var (quotation, rfqId) = await ArrangeRevisionDenialAsync(scenario);
        var originalCurrentVersionId = quotation.CurrentVersionId;
        DbContext.ChangeTracker.Clear();
        var before = await CaptureEffectsAsync(DbContext);
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var pdfClient = new Mock<IPdfServiceClient>(MockBehavior.Strict);
        var publisher = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object, pdfClient: pdfClient.Object, publisher: publisher.Object);

        var exception = await Assert.ThrowsAsync<RfqNotFoundException>(() => service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Denied RFQ revision",
            currentUserId: "test-user"));

        Assert.Equal($"RFQ with ID {rfqId} not found", exception.Message);
        Assert.Equal(before, await CaptureEffectsAsync(DbContext));
        DbContext.ChangeTracker.Clear();
        var unchanged = await DbContext.Quotations.AsNoTracking().SingleAsync(item => item.Id == quotation.Id);
        Assert.Equal(originalCurrentVersionId, unchanged.CurrentVersionId);
        projectClient.VerifyNoOtherCalls();
        pdfClient.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_OwnedConvertedRfq_CreatesExactlyVersionTwo()
    {
        var customer = await AddCustomerAsync("revision-owner");
        var rfq = await AddRfqAsync(customer, RfqStatus.Qualified);
        var quotation = await AddQuotationWithVersionAsync(customer, rfq.Id);
        rfq.Status = RfqStatus.Converted;
        rfq.ConvertedToQuotationId = quotation.Id;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object);

        await service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Owned RFQ revision",
            currentUserId: "test-user");

        var versions = await DbContext.QuotationVersions
            .AsNoTracking()
            .Where(item => item.QuotationId == quotation.Id)
            .OrderBy(item => item.VersionNumber)
            .ToListAsync();
        Assert.Collection(
            versions,
            first => Assert.Equal(1, first.VersionNumber),
            second => Assert.Equal(2, second.VersionNumber));
        projectClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_NoSourceRfq_RemainsCompatible()
    {
        var customer = await AddCustomerAsync("unlinked-revision");
        var quotation = await AddQuotationWithVersionAsync(customer, sourceRfqId: null);
        DbContext.ChangeTracker.Clear();
        var projectClient = new Mock<IProjectServiceClient>(MockBehavior.Strict);
        var service = CreateService(DbContext, projectClient.Object);

        await service.UpdateAsync(
            quotation.Id,
            CreateLineItems(),
            "Unlinked revision",
            currentUserId: "test-user");

        Assert.Equal(2, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == quotation.Id));
        projectClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_TwoConcurrentClaims_ExactlyOneQuotationWins()
    {
        var customer = await AddCustomerAsync("concurrent");
        var rfq = await AddRfqAsync(customer, RfqStatus.Qualified);
        DbContext.ChangeTracker.Clear();
        var barrier = new RfqEligibilitySelectBarrier();
        var connectionString = DbContext.Database.GetConnectionString();
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        var options = new DbContextOptionsBuilder<QuotationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(QuotationDbContext).Assembly.GetName().Name))
            .AddInterceptors(barrier)
            .Options;
        await using var firstContext = new QuotationDbContext(options);
        await using var secondContext = new QuotationDbContext(options);
        var firstService = CreateService(firstContext, Mock.Of<IProjectServiceClient>());
        var secondService = CreateService(secondContext, Mock.Of<IProjectServiceClient>());

        var attempts = await Task.WhenAll(
                CaptureCreateAttemptAsync(firstService, customer.Id, rfq.Id),
                CaptureCreateAttemptAsync(secondService, customer.Id, rfq.Id))
            .WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(2, barrier.Arrivals);
        var winner = Assert.Single(attempts, result => result.Quotation is not null);
        var loser = Assert.Single(attempts, result => result.Exception is not null);
        Assert.IsType<RfqNotFoundException>(loser.Exception);
        DbContext.ChangeTracker.Clear();
        var converted = await DbContext.Rfqs.AsNoTracking().SingleAsync(item => item.Id == rfq.Id);
        Assert.Equal(RfqStatus.Converted, converted.Status);
        Assert.Equal(winner.Quotation!.Id, converted.ConvertedToQuotationId);
        Assert.Equal(1, await DbContext.Quotations.CountAsync(item => item.SourceRfqId == rfq.Id));
        Assert.Equal(1, await DbContext.QuotationVersions.CountAsync(item => item.QuotationId == winner.Quotation.Id));
    }

    private Api.Services.QuotationService CreateService(
        QuotationDbContext context,
        IProjectServiceClient projectClient,
        ICustomerServiceClient? customerClient = null,
        IPdfServiceClient? pdfClient = null,
        IPublishEndpoint? publisher = null) =>
        new(
            context,
            _logger.Object,
            _metrics,
            publisher ?? Mock.Of<IPublishEndpoint>(),
            projectClient,
            customerClient,
            pdfClient);

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

    private async Task<(Guid CustomerId, Guid RfqId)> ArrangeCreateDenialAsync(CreateDenialScenario scenario)
    {
        if (scenario == CreateDenialScenario.Missing)
        {
            return (Guid.NewGuid(), Guid.NewGuid());
        }

        if (scenario == CreateDenialScenario.Empty)
        {
            return (Guid.NewGuid(), Guid.Empty);
        }

        var customer = await AddCustomerAsync($"create-{scenario}");
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

        return (customer.Id, rfq.Id);
    }

    private async Task<(Quotation Quotation, Guid RfqId)> ArrangeRevisionDenialAsync(
        RevisionDenialScenario scenario)
    {
        var customer = await AddCustomerAsync($"revision-{scenario}");
        if (scenario == RevisionDenialScenario.Empty)
        {
            await InsertEmptyRfqAsync(customer.Id);
            var emptyQuotation = await AddQuotationWithVersionAsync(customer, Guid.Empty);
            await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE rfqs SET status = {(int)RfqStatus.Converted}, converted_to_quotation_id = {emptyQuotation.Id} WHERE id = {Guid.Empty}");
            return (emptyQuotation, Guid.Empty);
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
        return (quotation, rfq.Id);
    }

    private Task InsertEmptyRfqAsync(Guid customerId) =>
        DbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO rfqs (id, customer_id, channel_source, status, created_at, updated_at, is_deleted)
            VALUES ({Guid.Empty}, {customerId}, {(int)RfqChannel.Website}, {(int)RfqStatus.Converted}, {DateTime.UtcNow}, {DateTime.UtcNow}, FALSE)
            """);

    private async Task<CreateAttempt> CaptureCreateAttemptAsync(
        Api.Services.QuotationService service,
        Guid customerId,
        Guid rfqId)
    {
        try
        {
            var quotation = await service.CreateAsync(
                customerId,
                rfqId,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                CreateLineItems(),
                deliveryExpectations: null,
                currentUserId: "concurrent-user");
            return new CreateAttempt(quotation, null);
        }
        catch (Exception ex)
        {
            return new CreateAttempt(null, ex);
        }
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

    private static async Task<EffectCounts> CaptureEffectsAsync(QuotationDbContext context) => new(
        await context.Customers.CountAsync(),
        await context.Rfqs.IgnoreQueryFilters().CountAsync(),
        await context.Quotations.CountAsync(),
        await context.QuotationVersions.CountAsync(),
        await context.QuotationLineItems.CountAsync(),
        await context.DiscountStructures.CountAsync(),
        await context.AuditLogEntries.CountAsync(),
        await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM outbox_message").SingleAsync());

    private sealed record EffectCounts(
        int Customers,
        int Rfqs,
        int Quotations,
        int Versions,
        int LineItems,
        int Discounts,
        int Audits,
        int OutboxMessages);

    private sealed record CreateAttempt(Quotation? Quotation, Exception? Exception);

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

    private sealed class RfqEligibilitySelectBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public int Arrivals => Volatile.Read(ref _arrivals);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (!IsEligibilitySelect(command.CommandText))
            {
                return result;
            }

            var arrival = Interlocked.Increment(ref _arrivals);
            if (arrival == 2)
            {
                _bothArrived.TrySetResult();
            }

            if (arrival <= 2)
            {
                await _bothArrived.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return result;
        }

        private static bool IsEligibilitySelect(string commandText) =>
            commandText.Contains("SELECT EXISTS", StringComparison.OrdinalIgnoreCase) &&
            commandText.Contains("rfqs", StringComparison.OrdinalIgnoreCase) &&
            commandText.Contains("converted_to_quotation_id", StringComparison.OrdinalIgnoreCase) &&
            commandText.Contains("customer_id", StringComparison.OrdinalIgnoreCase);
    }
}
