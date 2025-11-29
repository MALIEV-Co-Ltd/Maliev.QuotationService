using System.Text.Json;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Services;

public class RfqService : IRfqService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<RfqService> _logger;

    public RfqService(QuotationDbContext context, ILogger<RfqService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Rfq> CreateAsync(
        Guid customerId,
        RfqChannel channelSource,
        object requestDetails,
        List<Guid>? uploadServiceFileIds,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating RFQ for customer {CustomerId} from channel {ChannelSource}", customerId, channelSource);

        // Verify customer exists
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new KeyNotFoundException($"Customer with ID {customerId} not found");
        }

        // Create RFQ
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ChannelSource = channelSource,
            Status = RfqStatus.New,
            RequestDetails = JsonDocument.Parse(JsonSerializer.Serialize(requestDetails)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Rfqs.Add(rfq);

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfq.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Create,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                CustomerId = customerId,
                ChannelSource = channelSource.ToString(),
                Status = RfqStatus.New.ToString()
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        // TODO: Handle file references if uploadServiceFileIds is provided
        // This will be implemented in Phase 6 (External Integration)

        await _context.SaveChangesAsync(cancellationToken);

        // Emit metric
        BusinessMetrics.RfqCreatedTotal.WithLabels(channelSource.ToString()).Inc();

        _logger.LogInformation("Created RFQ {RfqId} for customer {CustomerId}", rfq.Id, customerId);

        return rfq;
    }

    public async Task<Rfq?> GetByIdAsync(Guid rfqId, CancellationToken cancellationToken = default)
    {
        return await _context.Rfqs
            .Include(r => r.Customer)
            .Include(r => r.FileReferences)
            .Include(r => r.InternalNotes)
            .FirstOrDefaultAsync(r => r.Id == rfqId, cancellationToken);
    }

    public async Task<(List<Rfq> Rfqs, int TotalCount)> GetAllAsync(
        RfqChannel? channelSource = null,
        RfqStatus? status = null,
        Guid? customerId = null,
        string? assignedStaffUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rfqs
            .Include(r => r.Customer)
            .AsQueryable();

        // Apply filters
        if (channelSource.HasValue)
        {
            query = query.Where(r => r.ChannelSource == channelSource.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedStaffUserId))
        {
            query = query.Where(r => r.AssignedStaffUserId == assignedStaffUserId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var rfqs = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (rfqs, totalCount);
    }

    public async Task<Rfq> UpdateAsync(
        Guid rfqId,
        object? requestDetails,
        string? assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var changedFields = new Dictionary<string, object?>();

        if (requestDetails != null)
        {
            rfq.RequestDetails = JsonDocument.Parse(JsonSerializer.Serialize(requestDetails));
            changedFields["RequestDetails"] = requestDetails;
        }

        if (assignedStaffUserId != null && rfq.AssignedStaffUserId != assignedStaffUserId)
        {
            changedFields["AssignedStaffUserId"] = new { Old = rfq.AssignedStaffUserId, New = assignedStaffUserId };
            rfq.AssignedStaffUserId = assignedStaffUserId;
        }

        rfq.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry
        if (changedFields.Count > 0)
        {
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = AuditEntityType.RFQ,
                EntityId = rfq.Id,
                UserId = currentUserId,
                ActionType = AuditActionType.Update,
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(changedFields))
            };

            _context.AuditLogEntries.Add(auditEntry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated RFQ {RfqId}", rfqId);

        return rfq;
    }

    public async Task<Rfq> UpdateStatusAsync(
        Guid rfqId,
        RfqStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var oldStatus = rfq.Status;

        // Validate status transition
        if (!IsValidStatusTransition(oldStatus, status))
        {
            throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {status}");
        }

        rfq.Status = status;
        rfq.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfq.Id,
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
        BusinessMetrics.RfqStatusTransitionsTotal
            .WithLabels(oldStatus.ToString(), status.ToString())
            .Inc();

        _logger.LogInformation("Updated RFQ {RfqId} status from {OldStatus} to {NewStatus}", rfqId, oldStatus, status);

        return rfq;
    }

    public async Task<InternalNote> AddNoteAsync(
        Guid rfqId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfqExists = await _context.Rfqs.AnyAsync(r => r.Id == rfqId, cancellationToken);
        if (!rfqExists)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var note = new InternalNote
        {
            Id = Guid.NewGuid(),
            RfqId = rfqId,
            AuthorUserId = currentUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.InternalNotes.Add(note);

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.InternalNote,
            EntityId = note.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Create,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                RfqId = rfqId,
                Content = content.Length > 100 ? content[..100] + "..." : content
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        // Emit metric
        BusinessMetrics.InternalNotesCreatedTotal.Inc();

        _logger.LogInformation("Added note to RFQ {RfqId}", rfqId);

        return note;
    }

    public async Task<Rfq> AssignAsync(
        Guid rfqId,
        string assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var oldAssignee = rfq.AssignedStaffUserId;
        rfq.AssignedStaffUserId = assignedStaffUserId;
        rfq.UpdatedAt = DateTime.UtcNow;

        // Update status to InProgress if it was New
        if (rfq.Status == RfqStatus.New)
        {
            rfq.Status = RfqStatus.InProgress;
        }

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfq.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Update,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                AssignedStaffUserId = new { Old = oldAssignee, New = assignedStaffUserId },
                Status = rfq.Status == RfqStatus.InProgress ? "New -> InProgress" : null
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned RFQ {RfqId} to staff user {StaffUserId}", rfqId, assignedStaffUserId);

        return rfq;
    }

    public async Task<Guid> ConvertToQuotationAsync(
        Guid rfqId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == rfqId, cancellationToken);

        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        // This will be implemented in Phase 4 (Quotation Creation)
        // For now, just update the RFQ status
        rfq.Status = RfqStatus.Converted;
        rfq.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Converted RFQ {RfqId} to quotation", rfqId);

        // Return a placeholder quotation ID
        return Guid.NewGuid();
    }

    private static bool IsValidStatusTransition(RfqStatus from, RfqStatus to)
    {
        // Define valid status transitions
        return (from, to) switch
        {
            (RfqStatus.New, RfqStatus.InProgress) => true,
            (RfqStatus.New, RfqStatus.Abandoned) => true,
            (RfqStatus.InProgress, RfqStatus.Qualified) => true,
            (RfqStatus.InProgress, RfqStatus.Converted) => true,
            (RfqStatus.InProgress, RfqStatus.Abandoned) => true,
            (RfqStatus.Qualified, RfqStatus.Converted) => true,
            (RfqStatus.Qualified, RfqStatus.Abandoned) => true,
            _ => from == to // Allow same status
        };
    }
}
