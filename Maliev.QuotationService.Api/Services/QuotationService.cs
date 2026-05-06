using Maliev.MessagingContracts.Contracts.Quotations;
using Maliev.MessagingContracts;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.QuotationService.Api.Services;

/// <summary>
/// Implementation of the quotation management service.
/// </summary>
public class QuotationService : IQuotationService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<QuotationService> _logger;
    private readonly MetricsService _metricsService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICustomerServiceClient? _customerServiceClient;

    /// <summary>
    /// Initializes a new instance of the QuotationService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="metricsService">The metrics service.</param>
    /// <param name="publishEndpoint">The publish endpoint for messaging.</param>
    /// <param name="customerServiceClient">The CustomerService client used to hydrate local customer references.</param>
    public QuotationService(
        QuotationDbContext context,
        ILogger<QuotationService> logger,
        MetricsService metricsService,
        IPublishEndpoint publishEndpoint,
        ICustomerServiceClient? customerServiceClient = null)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
        _publishEndpoint = publishEndpoint;
        _customerServiceClient = customerServiceClient;
    }

    /// <summary>
    /// Creates a new quotation.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="sourceRfqId">The unique identifier of the source RFQ, if any.</param>
    /// <param name="validityPeriodStart">The start date of the validity period.</param>
    /// <param name="validityPeriodEnd">The end date of the validity period.</param>
    /// <param name="lineItems">The line items for the quotation.</param>
    /// <param name="deliveryExpectations">Delivery expectations or notes.</param>
    /// <param name="currentUserId">The user ID creating the quotation.</param>
    /// <param name="billingIdentityType">The billing identity type.</param>
    /// <param name="discountStructure">The discount structure to apply, if any.</param>
    /// <param name="manualDiscountAmount">The manual discount amount to apply.</param>
    /// <param name="shippingCost">The shipping or delivery cost to apply.</param>
    /// <param name="taxAmount">The VAT or tax amount to apply.</param>
    /// <param name="specialTerms">Customer-facing special terms for generated PDFs.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created quotation.</returns>
    public async Task<Quotation> CreateAsync(
        Guid customerId,
        Guid? sourceRfqId,
        DateTime validityPeriodStart,
        DateTime validityPeriodEnd,
        IEnumerable<QuotationLineItemDto> lineItems,
        string? deliveryExpectations,
        string currentUserId,
        Domain.Enums.BillingIdentityType billingIdentityType = Domain.Enums.BillingIdentityType.Corporate,
        DiscountStructureDto? discountStructure = null,
        decimal manualDiscountAmount = 0m,
        decimal shippingCost = 0m,
        decimal taxAmount = 0m,
        string? specialTerms = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating quotation for customer {CustomerId}", customerId);

        await EnsureCustomerReferenceAsync(customerId, cancellationToken);

        // Verify RFQ exists if provided
        if (sourceRfqId.HasValue)
        {
            var rfqExists = await _context.Rfqs.AnyAsync(r => r.Id == sourceRfqId.Value, cancellationToken);
            if (!rfqExists)
            {
                throw new KeyNotFoundException($"RFQ with ID {sourceRfqId.Value} not found");
            }
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Create quotation
                var quotation = new Quotation
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    SourceRfqId = sourceRfqId,
                    Status = QuotationStatus.Draft,
                    BillingIdentityType = billingIdentityType,
                    ValidityPeriodStart = DateOnly.FromDateTime(validityPeriodStart),
                    ValidityPeriodEnd = DateOnly.FromDateTime(validityPeriodEnd),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Quotations.Add(quotation);

                // Save quotation first without CurrentVersionId to avoid circular dependency
                await _context.SaveChangesAsync(cancellationToken);

                // Create first version after quotation is saved
                var version = await CreateVersionAsync(
                    quotation.Id,
                    1,
                    lineItems,
                    deliveryExpectations,
                    currentUserId,
                    discountStructure,
                    manualDiscountAmount,
                    shippingCost,
                    taxAmount,
                    specialTerms,
                    "Initial version",
                    cancellationToken);

                // Now update the quotation with the current version ID
                quotation.CurrentVersionId = version.Id;

                // Update RFQ if linked
                if (sourceRfqId.HasValue)
                {
                    var rfq = await _context.Rfqs.FindAsync(new object[] { sourceRfqId.Value }, cancellationToken);
                    if (rfq != null)
                    {
                        rfq.ConvertedToQuotationId = quotation.Id;
                        rfq.Status = RfqStatus.Converted;
                        rfq.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Create audit log entry
                var auditEntry = new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    EntityType = AuditEntityType.Quotation,
                    EntityId = quotation.Id,
                    UserId = currentUserId,
                    ActionType = AuditActionType.Create,
                    Timestamp = DateTime.UtcNow,
                    ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        CustomerId = customerId,
                        SourceRfqId = sourceRfqId,
                        Status = QuotationStatus.Draft.ToString()
                    }))
                };

                _context.AuditLogEntries.Add(auditEntry);

                // Save version, audit entry, and updated quotation
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                // Emit metric
                _metricsService.RecordQuotationCreated();

                _logger.LogInformation("Created quotation {QuotationId} for customer {CustomerId}", quotation.Id, customerId);

                // Publish QuotationCreatedEvent
                await _publishEndpoint.Publish(new QuotationCreatedEvent(
                    MessageId: Guid.NewGuid(),
                    MessageName: "QuotationCreatedEvent",
                    MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
                    MessageVersion: "1.0.0",
                    PublishedBy: "QuotationService",
                    ConsumedBy: ["NotificationService", "AnalyticsService"],
                    CorrelationId: Guid.NewGuid(),
                    CausationId: null,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    IsPublic: false,
                    Payload: new QuotationCreatedEventPayload(
                        QuotationId: quotation.Id,
                        QuotationNumber: quotation.Id.ToString(),
                        CustomerId: quotation.CustomerId,
                        TotalAmount: (double)version.TotalPrice,
                        Currency: version.CurrencyCode,
                        ValidUntil: new DateTimeOffset(quotation.ValidityPeriodEnd.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                        CreatedBy: currentUserId,
                        CreatedAt: new DateTimeOffset(quotation.CreatedAt, TimeSpan.Zero)
                    )
                ), cancellationToken);

                return quotation;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create quotation for customer {CustomerId}", customerId);
                throw;
            }
        });
    }

    /// <summary>
    /// Retrieves a quotation by its unique identifier.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The quotation if found, otherwise null.</returns>
    public async Task<Quotation?> GetByIdAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        return await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Versions)
                .ThenInclude(v => v.LineItems)
            .Include(q => q.Versions)
                .ThenInclude(v => v.DiscountStructures)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
    }

    /// <summary>
    /// Retrieves all quotations with optional filtering.
    /// </summary>
    /// <param name="status">Filter by quotation status.</param>
    /// <param name="customerId">Filter by customer ID.</param>
    /// <param name="fromDate">Filter by creation date (start).</param>
    /// <param name="toDate">Filter by creation date (end).</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The page size for pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the list of quotations and the total count.</returns>
    public async Task<(List<Quotation> Quotations, int TotalCount)> GetAllAsync(
        QuotationStatus? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Quotations
            .Include(q => q.Customer)
            .AsQueryable();

        // Apply filters
        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(q => q.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(q => q.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(q => q.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var quotations = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (quotations, totalCount);
    }

    /// <summary>
    /// Updates an existing quotation by creating a new version.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="lineItems">The updated line items.</param>
    /// <param name="changeSummary">A summary of the changes.</param>
    /// <param name="deliveryExpectations">Updated delivery expectations.</param>
    /// <param name="discountStructure">The updated discount structure.</param>
    /// <param name="manualDiscountAmount">The manual discount amount to apply.</param>
    /// <param name="shippingCost">The shipping or delivery cost to apply.</param>
    /// <param name="taxAmount">The VAT or tax amount to apply.</param>
    /// <param name="specialTerms">Customer-facing special terms for generated PDFs.</param>
    /// <param name="currentUserId">The user ID making the update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated quotation.</returns>
    public async Task<Quotation> UpdateAsync(
        Guid quotationId,
        IEnumerable<QuotationLineItemDto>? lineItems,
        string changeSummary,
        string? deliveryExpectations = null,
        DiscountStructureDto? discountStructure = null,
        decimal manualDiscountAmount = 0m,
        decimal shippingCost = 0m,
        decimal taxAmount = 0m,
        string? specialTerms = null,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        try
        {
            // Get current version number
            var currentVersionNumber = quotation.Versions.Any() ? quotation.Versions.Max(v => v.VersionNumber) : 0;
            var newVersionNumber = currentVersionNumber + 1;

            // Create new version
            var newVersion = await CreateVersionAsync(
                quotation.Id,
                newVersionNumber,
                lineItems ?? new List<QuotationLineItemDto>(),
                deliveryExpectations,
                currentUserId ?? "system",
                discountStructure,
                manualDiscountAmount,
                shippingCost,
                taxAmount,
                specialTerms,
                changeSummary,
                cancellationToken);

            quotation.CurrentVersionId = newVersion.Id;
            quotation.UpdatedAt = DateTime.UtcNow;

            // Create audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = AuditEntityType.QuotationVersion,
                EntityId = newVersion.Id,
                UserId = currentUserId ?? "system",
                ActionType = AuditActionType.Create,
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    QuotationId = quotationId,
                    VersionNumber = newVersionNumber,
                    ChangeSummary = changeSummary
                }))
            };

            _context.AuditLogEntries.Add(auditEntry);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created version {VersionNumber} for quotation {QuotationId}", newVersionNumber, quotationId);

            return quotation;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict updating quotation {QuotationId}", quotationId);
            throw;
        }
    }

    /// <summary>
    /// Updates the status of a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="status">The new status.</param>
    /// <param name="currentUserId">The user ID making the update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated quotation.</returns>
    public async Task<Quotation> UpdateStatusAsync(
        Guid quotationId,
        QuotationStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations.FindAsync(new object[] { quotationId }, cancellationToken);
        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        var oldStatus = quotation.Status;

        // Validate status transition using QuotationStateMachine
        if (!QuotationStateMachine.IsValidTransition(oldStatus, status))
        {
            throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {status}");
        }

        try
        {
            quotation.Status = status;
            quotation.UpdatedAt = DateTime.UtcNow;

            // Create audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = AuditEntityType.Quotation,
                EntityId = quotation.Id,
                UserId = currentUserId,
                ActionType = AuditActionType.Update,
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    Status = new { Old = oldStatus.ToString(), New = status.ToString() }
                }))
            };

            _context.AuditLogEntries.Add(auditEntry);

            await _context.SaveChangesAsync(cancellationToken);

            // Emit metric
            _metricsService.RecordQuotationStatusTransition(oldStatus.ToString(), status.ToString());

            _logger.LogInformation("Updated quotation {QuotationId} status from {OldStatus} to {NewStatus}",
                quotationId, oldStatus, status);

            // Publish status-specific events
            if (status == QuotationStatus.Accepted)
            {
                // Reload quotation with navigation properties to get current version data
                var quotationWithVersion = await _context.Quotations
                    .Include(q => q.CurrentVersion)
                    .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

                if (quotationWithVersion?.CurrentVersion != null)
                {
                    await _publishEndpoint.Publish(new QuotationAcceptedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "QuotationAcceptedEvent",
                        MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "QuotationService",
                        ConsumedBy: ["OrderService", "NotificationService", "AnalyticsService"],
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: DateTimeOffset.UtcNow,
                        IsPublic: false,
                        Payload: new QuotationAcceptedEventPayload(
                            QuotationId: quotation.Id,
                            QuotationNumber: quotation.Id.ToString(),
                            CustomerId: quotation.CustomerId,
                            AcceptedAmount: (double)quotationWithVersion.CurrentVersion.TotalPrice,
                            Currency: quotationWithVersion.CurrentVersion.CurrencyCode,
                            AcceptedAt: DateTimeOffset.UtcNow,
                            AcceptedBy: currentUserId
                        )
                    ), cancellationToken);
                }
            }

            return quotation;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict updating status for quotation {QuotationId}", quotationId);
            throw;
        }
    }

    /// <summary>
    /// Approves a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="currentUserId">The user ID approving the quotation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The approved quotation.</returns>
    public async Task<Quotation> ApproveAsync(
        Guid quotationId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations.FindAsync(new object[] { quotationId }, cancellationToken);
        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        // Validate current status is PendingApproval
        if (quotation.Status != QuotationStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Quotation must be in PendingApproval status to approve. Current status: {quotation.Status}");
        }

        var oldStatus = quotation.Status;
        quotation.Status = QuotationStatus.Approved;
        quotation.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.Quotation,
            EntityId = quotation.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Update,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                Status = new { Old = oldStatus.ToString(), New = QuotationStatus.Approved.ToString() },
                ApprovedBy = currentUserId
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        // Emit metrics
        _metricsService.RecordQuotationApproved();
        _metricsService.RecordQuotationStatusTransition(oldStatus.ToString(), QuotationStatus.Approved.ToString());

        _logger.LogInformation(
            "Quotation {QuotationId} approved by {UserId}. Status changed from {OldStatus} to {NewStatus}",
            quotationId, currentUserId, oldStatus, QuotationStatus.Approved);

        return quotation;
    }

    /// <summary>
    /// Adds an internal note to a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="content">The content of the note.</param>
    /// <param name="currentUserId">The user ID adding the note.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created note.</returns>
    public async Task<InternalNote> AddNoteAsync(
        Guid quotationId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var quotationExists = await _context.Quotations.AnyAsync(q => q.Id == quotationId, cancellationToken);
        if (!quotationExists)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        var note = new InternalNote
        {
            Id = Guid.NewGuid(),
            QuotationId = quotationId,
            AuthorUserId = currentUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.InternalNotes.Add(note);

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.Quotation,
            EntityId = quotationId,
            UserId = currentUserId,
            ActionType = AuditActionType.NoteAdded,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                NoteAdded = new { Content = content.Length > 50 ? content[..50] + "..." : content }
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);
        await _context.SaveChangesAsync(cancellationToken);

        // Emit metric
        _metricsService.RecordInternalNoteCreated();

        return note;
    }

    /// <summary>
    /// Gets all versions of a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of versions.</returns>
    public async Task<List<QuotationVersion>> GetVersionsAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuotationVersions
            .Where(v => v.QuotationId == quotationId)
            .Include(v => v.LineItems)
            .Include(v => v.DiscountStructures)
            .OrderByDescending(v => v.VersionNumber)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a specific version of a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="versionNumber">The version number to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The version if found, otherwise null.</returns>
    public async Task<QuotationVersion?> GetVersionByNumberAsync(
        Guid quotationId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuotationVersions
            .Include(v => v.LineItems)
            .Include(v => v.DiscountStructures)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.QuotationId == quotationId && v.VersionNumber == versionNumber, cancellationToken);
    }

    /// <summary>
    /// Generates a PDF for a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="versionNumber">The specific version number to generate PDF for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PDF bytes.</returns>
    public async Task<byte[]> GeneratePdfAsync(
        Guid quotationId,
        int? versionNumber = null,
        CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Versions)
                .ThenInclude(v => v.LineItems)
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        // In a real implementation, we would call the PdfServiceClient here
        _logger.LogInformation("Generating PDF for quotation {QuotationId}", quotationId);

        return Array.Empty<byte>();
    }

    /// <summary>
    /// Deletes a quotation.
    /// </summary>
    /// <param name="quotationId">The unique identifier of the quotation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations.FindAsync(new object[] { quotationId }, cancellationToken);
        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        quotation.IsDeleted = true;
        quotation.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted quotation {QuotationId}", quotationId);
    }

    private async Task EnsureCustomerReferenceAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken);
        if (customerExists)
            return;

        if (_customerServiceClient is null)
            throw new KeyNotFoundException($"Customer with ID {customerId} not found");

        var customer = await _customerServiceClient.GetCustomerByIdAsync(customerId, cancellationToken);
        if (customer is null)
            throw new KeyNotFoundException($"Customer with ID {customerId} not found");

        var customerName = FirstNonEmpty(
            customer.Name,
            $"{customer.FirstName} {customer.LastName}".Trim(),
            customer.CompanyName,
            customer.Email,
            customerId.ToString("N"))!;

        var phoneNumber = FirstNonEmpty(customer.Mobile, customer.Landline, customer.CompanyPhone);

        _context.Customers.Add(new Customer
        {
            Id = customer.Id,
            Email = customer.Email,
            PhoneNumber = phoneNumber,
            Name = customerName,
            ContactInfo = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                customer.CompanyId,
                customer.CompanyName
            })),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private async Task<QuotationVersion> CreateVersionAsync(
        Guid quotationId,
        int versionNumber,
        IEnumerable<QuotationLineItemDto> lineItems,
        string? deliveryExpectations,
        string createdByUserId,
        DiscountStructureDto? discountStructure,
        decimal manualDiscountAmount,
        decimal shippingCost,
        decimal taxAmount,
        string? specialTerms,
        string changeSummary,
        CancellationToken cancellationToken)
    {
        var version = new QuotationVersion
        {
            Id = Guid.NewGuid(),
            QuotationId = quotationId,
            VersionNumber = versionNumber,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            ChangeSummary = changeSummary,
            TotalPrice = 0, // Will be calculated
            CurrencyCode = "THB",
            ManualDiscountAmount = Math.Max(0m, manualDiscountAmount),
            ShippingCost = Math.Max(0m, shippingCost),
            TaxAmount = Math.Max(0m, taxAmount),
            SpecialTerms = string.IsNullOrWhiteSpace(specialTerms) ? null : specialTerms,
            DeliveryExpectations = !string.IsNullOrEmpty(deliveryExpectations)
                ? JsonDocument.Parse(JsonSerializer.Serialize(new { expectations = deliveryExpectations }))
                : null
        };

        _context.QuotationVersions.Add(version);

        // Create line items
        decimal totalPrice = 0;
        int lineNumber = 1;

        foreach (var item in lineItems)
        {
            var lineTotal = item.Quantity * item.UnitPrice;

            var lineItem = new QuotationLineItem
            {
                Id = Guid.NewGuid(),
                VersionId = version.Id,
                LineNumber = lineNumber++,
                MaterialServiceId = item.MaterialServiceId,
                MaterialName = "Material", // Should be fetched from Material Service
                Quantity = item.Quantity,
                QuantityUnit = item.UnitOfMeasure,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal,
                ManufacturingProcess = item.ManufacturingProcess,
                Notes = item.Notes
            };

            _context.QuotationLineItems.Add(lineItem);
            totalPrice += lineTotal;
        }

        // Create discount if provided
        if (discountStructure != null)
        {
            var discount = new DiscountStructure
            {
                Id = Guid.NewGuid(),
                QuotationVersionId = version.Id,
                DiscountType = discountStructure.DiscountType,
                DiscountValue = discountStructure.DiscountValue,
                Conditions = discountStructure.Conditions,
                AuthorizationReason = discountStructure.AuthorizationReason
            };

            _context.DiscountStructures.Add(discount);

            // Apply discount to total price with explicit rounding
            if (discount.DiscountType == DiscountType.Percentage)
            {
                var discountAmount = decimal.Round(totalPrice * (discount.DiscountValue / 100), 2, MidpointRounding.AwayFromZero);
                totalPrice -= discountAmount;
            }
            else if (discount.DiscountType == DiscountType.FixedAmount)
            {
                totalPrice -= discount.DiscountValue;
            }
            else if (discount.DiscountType == DiscountType.VolumeBased)
            {
                // In a real implementation, volume-based discounts would be calculated
                // based on line item quantities and predefined tiers.
                _logger.LogInformation("Applying volume-based discount for quotation {QuotationId}", quotationId);
                var discountAmount = decimal.Round(totalPrice * (discount.DiscountValue / 100), 2, MidpointRounding.AwayFromZero);
                totalPrice -= discountAmount;
            }
        }

        var manualDiscount = Math.Min(Math.Max(0m, manualDiscountAmount), Math.Max(0m, totalPrice));
        totalPrice = Math.Max(0m, totalPrice - manualDiscount);
        version.TotalPrice = totalPrice + Math.Max(0m, shippingCost) + Math.Max(0m, taxAmount);

        return version;
    }
}
