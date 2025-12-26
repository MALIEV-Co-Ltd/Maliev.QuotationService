using System.Text.Json;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Services;

public class QuotationService : IQuotationService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<QuotationService> _logger;
    private readonly MetricsService _metricsService;

    public QuotationService(
        QuotationDbContext context,
        ILogger<QuotationService> logger,
        MetricsService metricsService)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    // NOTE: Automatic expiration of quotations (CustomerReview → Expired when ValidityPeriodEnd < NOW)
    // should be handled by a background job or scheduled task (e.g., using Hangfire, Quartz.NET, or
    // a timed hosted service). This ensures quotations are automatically expired when their validity
    // period ends without requiring manual intervention. The background job should:
    // 1. Query for quotations in CustomerReview status where ValidityPeriodEnd < UTC NOW
    // 2. Call UpdateStatusAsync to transition each to Expired status
    // 3. Run on a scheduled interval (e.g., hourly or daily depending on business requirements)
    // This is not implemented in this phase but should be added as a separate infrastructure component.

    public async Task<Quotation> CreateAsync(
        Guid customerId,
        Guid? sourceRfqId,
        DateTime validityPeriodStart,
        DateTime validityPeriodEnd,
        IEnumerable<object> lineItems,
        string? deliveryExpectations,
        string currentUserId,
        object? discountStructure = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating quotation for customer {CustomerId}", customerId);

        // Verify customer exists
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new KeyNotFoundException($"Customer with ID {customerId} not found");
        }

        // Verify RFQ exists if provided
        if (sourceRfqId.HasValue)
        {
            var rfqExists = await _context.Rfqs.AnyAsync(r => r.Id == sourceRfqId.Value, cancellationToken);
            if (!rfqExists)
            {
                throw new KeyNotFoundException($"RFQ with ID {sourceRfqId.Value} not found");
            }
        }

        // Create quotation
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SourceRfqId = sourceRfqId,
            Status = QuotationStatus.Draft,
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

        // Emit metric
        _metricsService.RecordQuotationCreated();

        _logger.LogInformation("Created quotation {QuotationId} for customer {CustomerId}", quotation.Id, customerId);

        return quotation;
    }

    public async Task<Quotation?> GetByIdAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        return await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Versions)
                .ThenInclude(v => v.LineItems)
            .Include(q => q.Versions)
                .ThenInclude(v => v.DiscountStructures)
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
    }

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

    public async Task<Quotation> UpdateAsync(
        Guid quotationId,
        IEnumerable<object>? lineItems,
        string changeSummary,
        string? deliveryExpectations = null,
        object? discountStructure = null,
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

        // Get current version number
        var currentVersionNumber = quotation.Versions.Max(v => v.VersionNumber);
        var newVersionNumber = currentVersionNumber + 1;

        // Create new version
        var newVersion = await CreateVersionAsync(
            quotation.Id,
            newVersionNumber,
            lineItems ?? new List<object>(),
            deliveryExpectations,
            currentUserId ?? "system",
            discountStructure,
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

        return quotation;
    }

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

    public async Task<List<QuotationVersion>> GetVersionsAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuotationVersions
            .Where(v => v.QuotationId == quotationId)
            .Include(v => v.LineItems)
            .Include(v => v.DiscountStructures)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuotationVersion?> GetVersionByNumberAsync(
        Guid quotationId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuotationVersions
            .Include(v => v.LineItems)
            .Include(v => v.DiscountStructures)
            .FirstOrDefaultAsync(v => v.QuotationId == quotationId && v.VersionNumber == versionNumber, cancellationToken);
    }

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

        // This will be implemented in Phase 6 (External Integration)
        _logger.LogInformation("PDF generation for quotation {QuotationId} (not yet implemented)", quotationId);

        // Return empty byte array as placeholder
        return Array.Empty<byte>();
    }

    public async Task DeleteAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Versions)
                .ThenInclude(v => v.LineItems)
            .Include(q => q.Versions)
                .ThenInclude(v => v.DiscountStructures)
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

        if (quotation == null)
        {
            throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
        }

        _context.Quotations.Remove(quotation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted quotation {QuotationId}", quotationId);
    }

    private async Task<QuotationVersion> CreateVersionAsync(
        Guid quotationId,
        int versionNumber,
        IEnumerable<object> lineItems,
        string? deliveryExpectations,
        string createdByUserId,
        object? discountStructure,
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
            var itemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(item));
            if (itemDict == null) continue;

            var materialId = itemDict["MaterialServiceId"].GetGuid();
            var quantity = itemDict["Quantity"].GetInt32();
            var unitPrice = itemDict["UnitPrice"].GetDecimal();
            var lineTotal = quantity * unitPrice;

            var lineItem = new QuotationLineItem
            {
                Id = Guid.NewGuid(),
                VersionId = version.Id,
                LineNumber = lineNumber++,
                MaterialServiceId = materialId,
                MaterialName = itemDict.ContainsKey("MaterialName") ? itemDict["MaterialName"].GetString() ?? "" : "",
                Quantity = quantity,
                QuantityUnit = itemDict.ContainsKey("UnitOfMeasure") ? itemDict["UnitOfMeasure"].GetString() ?? "pieces" : "pieces",
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                ManufacturingProcess = itemDict.ContainsKey("ManufacturingProcess") ? itemDict["ManufacturingProcess"].GetString() : null,
                Notes = itemDict.ContainsKey("Notes") ? itemDict["Notes"].GetString() : null
            };

            _context.QuotationLineItems.Add(lineItem);
            totalPrice += lineTotal;
        }

        // Create discount if provided
        if (discountStructure != null)
        {
            var discountDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(discountStructure));
            if (discountDict != null)
            {
                var discount = new DiscountStructure
                {
                    Id = Guid.NewGuid(),
                    QuotationVersionId = version.Id,
                    DiscountType = (DiscountType)discountDict["DiscountType"].GetInt32(),
                    DiscountValue = discountDict["DiscountValue"].GetDecimal(),
                    Conditions = discountDict.ContainsKey("Conditions") ? discountDict["Conditions"].GetString() : null,
                    AuthorizationReason = discountDict.ContainsKey("AuthorizationReason") ? discountDict["AuthorizationReason"].GetString() : null
                };

                _context.DiscountStructures.Add(discount);

                // Apply discount to total price
                if (discount.DiscountType == DiscountType.Percentage)
                {
                    totalPrice -= totalPrice * (discount.DiscountValue / 100);
                }
                else if (discount.DiscountType == DiscountType.FixedAmount)
                {
                    totalPrice -= discount.DiscountValue;
                }
            }
        }

        version.TotalPrice = totalPrice;

        return version;
    }

}
